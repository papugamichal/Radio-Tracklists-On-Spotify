using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.DTOs;
using RestSharp;
using RestSharp.Authenticators;

namespace RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction
{
    public interface ISpotifyAuthorizationService
    {
        Uri GetAuthorizationCodeUri();
        Task SetupAccessToken(string authorizationCode);
        bool IsAuthenticated();
        string GetToken();
    }
}

namespace RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Security
{
    public class SpotifyAuthorizationService : ISpotifyAuthorizationService, IDisposable
    {
        private readonly ILogger<SpotifyAuthorizationService> logger;
        private readonly IOptions<SpotifyAuthorizationServiceOptions> options;
        private readonly IDataProtector dataProtector;
        private readonly Timer refreshTokenTimer;
        private readonly TimeSpan defaultTimerInterval = TimeSpan.FromMinutes(1);
        private readonly AccessTokenDto tokenDto;
        private readonly string protectedTokenFilePath;
        private static object accessTokenLock = new object();

        private const string contentType = "content-type";
        private const string grantType = "grant_type";
        private const string codeHeader = "code";
        private const string redirectUri = "redirect_uri";
        private const string scope = "scope";
        private const string refreshToken = "refresh_token";
        private const string scopesToRequest = "playlist-modify-private playlist-read-private playlist-modify-public ugc-image-upload";

        public SpotifyAuthorizationService(
            ILogger<SpotifyAuthorizationService> logger,
            IOptions<SpotifyAuthorizationServiceOptions> options,
            IDataProtectionProvider dataProtector
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.dataProtector = (dataProtector ?? throw new ArgumentNullException(nameof(dataProtector))).CreateProtector("authorizationToken");
            this.refreshTokenTimer = new Timer(RefreshToken, null, TimeSpan.Zero, defaultTimerInterval);
            this.protectedTokenFilePath = Path.Combine(Directory.GetCurrentDirectory(), "accesstoken");
            this.tokenDto = InitializeAccessToken();
        }

        public async Task SetupAccessToken(string authorizationCode)
        {
            var client = new RestClient(options.Value.AccountWebApi);
            client.Authenticator = new HttpBasicAuthenticator(options.Value.ClientId, options.Value.ClientSecret);

            var postRequest = new RestRequest(options.Value.TokenEndpoint, Method.Post);
            postRequest.AddHeader(contentType, "application/x-www-form-urlencoded");
            postRequest.AddParameter(grantType, "authorization_code");
            postRequest.AddParameter(codeHeader, authorizationCode);
            postRequest.AddParameter(redirectUri, options.Value.RedirectUrl);

            var request = await client.ExecuteAsync<AccessTokenDto>(postRequest).ConfigureAwait(false);

            if (request.Data is null)
            {
                return;
            }

            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Access token request end with code: {request.StatusCode} Reason: {request.Content}");
                return;
            }

            tokenDto.SetCode(authorizationCode);
            tokenDto.Set(request.Data);
            var interval = TimeSpan.FromSeconds(request.Data.expires_in - 10);
            refreshTokenTimer?.Change(interval, interval);

            PersistSecurityToken(tokenDto);

            logger.LogInformation($"Access token has been setup. Token lifetime: {tokenDto.expires_in}s ({tokenDto.ExpireAtLocalTime()})");
        }

        public Uri GetAuthorizationCodeUri()
        {
            return new Uri(
                new Uri(options.Value.AccountWebApi),
                relativeUri: options.Value.AuthorizationEndpoint +
                    "?client_id=" + options.Value.ClientId +
                    "&response_type=code" +
                    "&redirect_uri=" + options.Value.RedirectUrl +
                    "&scope=" + HttpUtility.HtmlEncode(scopesToRequest) +
                    "&show_dialog=" + options.Value.ShowLoginDialog
                    );
        }

        public bool IsAuthenticated()
        {
            return true
                && this.tokenDto != null
                && !string.IsNullOrEmpty(this.tokenDto.autorizationCode)
                && !string.IsNullOrEmpty(this.tokenDto.access_token)
                && !this.tokenDto.IsExpired()
                ;
        }

        public string GetToken()
        {
            return this.tokenDto.access_token;
        }

        private void RefreshToken(object state)
        {
            if (!IsRefreshToken())
            {
                return;
            }

            logger.LogInformation("Access token is about to expire.");

            var client = new RestClient(options.Value.AccountWebApi);
            client.Authenticator = new HttpBasicAuthenticator(options.Value.ClientId, options.Value.ClientSecret);

            var postRequest = new RestRequest(options.Value.TokenEndpoint, Method.Post);
            postRequest.AddHeader(contentType, "application/x-www-form-urlencoded");
            postRequest.AddParameter(grantType, "refresh_token");
            postRequest.AddParameter(refreshToken, tokenDto.refresh_token);

            var request = client.ExecuteAsync<AccessTokenDto>(postRequest).ConfigureAwait(false).GetAwaiter().GetResult();

            if (request.Data is null)
            {
                return;
            }

            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Refresh access token request end with code: {request.StatusCode} Reason: {request.Content}");
                return;
            }

            tokenDto.Update(request.Data);
            var interval = TimeSpan.FromSeconds(request.Data.expires_in - 10);
            refreshTokenTimer?.Change(interval, interval);

            PersistSecurityToken(tokenDto);

            logger.LogInformation($"Access token has been refreshed. Token lifetime: {tokenDto.expires_in}s ({tokenDto.ExpireAtLocalTime()})");
        }

        private bool IsRefreshToken()
        {
            return !string.IsNullOrEmpty(this.tokenDto?.refresh_token);
        }

        private AccessTokenDto InitializeAccessToken()
        {
            return DecodeSecurityToken();
        }

        private void PersistSecurityToken(AccessTokenDto accessToken)
        {
            lock (accessTokenLock)
            {
                var rawToken = Newtonsoft.Json.JsonConvert.SerializeObject(accessToken);
                var protectedToken = this.dataProtector.Protect(rawToken);
                File.WriteAllText(protectedTokenFilePath, protectedToken);
            }
        }

        private AccessTokenDto DecodeSecurityToken()
        {
            lock (accessTokenLock)
            {
                if (!File.Exists(protectedTokenFilePath))
                {
                    return new AccessTokenDto();
                }
                else
                {
                    var raw = File.ReadAllText(protectedTokenFilePath);
                    var decryptedToken = this.dataProtector.Unprotect(raw);
                    var token = Newtonsoft.Json.JsonConvert.DeserializeObject<AccessTokenDto>(decryptedToken);
                    return token;
                }
            }
        }

        public void Dispose()
        {
            refreshTokenTimer?.Change(Timeout.Infinite, 0);
            refreshTokenTimer?.Dispose();
        }
    }
}
