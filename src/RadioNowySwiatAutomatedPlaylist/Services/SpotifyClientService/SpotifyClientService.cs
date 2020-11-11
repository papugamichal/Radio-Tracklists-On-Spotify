using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Configuration;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs.PlaylistTrack2;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serialization;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace RadioNowySwiatPlaylistBot.Services.SpotifyClientService
{
    public class SpotifyClientService : ISpotifyClientService, IDisposable
    {
        private readonly ILogger<SpotifyClientService> logger;
        private readonly IOptions<SpotifyClientOptions> iOptions;
        private SpotifyClientOptions options => iOptions.Value;

        private const string contentType = "content-type";
        private const string grantType = "grant_type";
        private const string codeHeader = "code";
        private const string redirectUri = "redirect_uri";
        private const string scope = "scope";
        private const string refreshToken = "refresh_token";
        private const string scopesToRequest = "playlist-modify-private playlist-read-private playlist-modify-public ugc-image-upload";

        private Timer refreshTokenTimer;
        private readonly TimeSpan defaultTimerInterval = TimeSpan.FromMinutes(1);

        private static string authorizationCode;
        private static readonly AccessTokenDto token = new AccessTokenDto();

        public SpotifyClientService(
            ILogger<SpotifyClientService> logger,
            IOptions<SpotifyClientOptions> options
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.iOptions = options ?? throw new ArgumentNullException(nameof(options));

            this.refreshTokenTimer = new Timer(RefreshToken, null, TimeSpan.Zero, defaultTimerInterval);
        }

        /* Authorization */
        public Uri GetAuthorizationCodeUrl()
        {
            return new Uri(new Uri(options.AccountWebApi),
                relativeUri: options.AuthorizationEndpoint +
                "?client_id=" + options.ClientId +
                "&response_type=code" +
                "&redirect_uri=" + options.RedirectUrl +
                "&scope=" + HttpUtility.HtmlEncode(scopesToRequest) +
                "&show_dialog=" + options.ShowLoginDialog);
        }

        public async Task SetupAccessToken()
        {
            var client = new RestClient(options.AccountWebApi);
            client.Authenticator = new HttpBasicAuthenticator(options.ClientId, options.ClientSecret);

            var postRequest = new RestRequest(options.TokenEndpoint, Method.POST);
            postRequest.AddHeader(contentType, "application/x-www-form-urlencoded");
            postRequest.AddParameter(grantType, "authorization_code");
            postRequest.AddParameter(codeHeader, authorizationCode);
            postRequest.AddParameter(redirectUri, options.RedirectUrl);

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

            token.Set(request.Data);
            var interval = TimeSpan.FromSeconds(request.Data.expires_in - 10);
            refreshTokenTimer?.Change(interval, interval);
            logger.LogInformation($"Access token has been setup. Token lifetime: {token.expires_in}s ({token.expires_in_datetime.ToLocalTime()})");
        }

        private void RefreshToken(object state)
        {
            if (!IsRefreshToken())
            {
                return;
            }

            logger.LogInformation("Access token is about to expire.");

            var client = new RestClient(options.AccountWebApi);
            client.Authenticator = new HttpBasicAuthenticator(options.ClientId, options.ClientSecret);

            var postRequest = new RestRequest(options.TokenEndpoint, Method.POST);
            postRequest.AddHeader(contentType, "application/x-www-form-urlencoded");
            postRequest.AddParameter(grantType, "refresh_token");
            postRequest.AddParameter(refreshToken, token.refresh_token);

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

            token.Update(request.Data);
            var interval = TimeSpan.FromSeconds(request.Data.expires_in - 10);
            refreshTokenTimer?.Change(interval, interval);

            logger.LogInformation($"Access token has been refreshed. Token lifetime: {token.expires_in}s ({token.expires_in_datetime.ToLocalTime()})");
        }

        public bool IsAuthenticated()
        {
            return true
                && !string.IsNullOrEmpty(authorizationCode)
                && !string.IsNullOrEmpty(token.access_token)
                && token?.expires_in_datetime >= DateTime.UtcNow;
        }

        public Task SetAuthorizationCode(string code)
        {
            authorizationCode = code;
            return Task.CompletedTask;
        }

        private bool IsRefreshToken()
        {
            return !string.IsNullOrEmpty(token.refresh_token);
        }

        /* API */
        public async Task<string> RequestForUserId()
        {
            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            var postRequest = new RestRequest("/v1/me", Method.GET);

            var request = await client.ExecuteAsync<UserDetailsDto>(postRequest).ConfigureAwait(false);
            if (request.Data is null)
            {
                return string.Empty;
            }
            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Request to '{request.ResponseUri}' end with code: {request.StatusCode} Reason: {request.Content}");
                return request.Content;
            }

            return request.Data.ID;
        }

        public async Task<IEnumerable<PlaylistDto>> RequestForUserPlaylists(bool publicOnly = false)
        {
            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            var postRequest = new RestRequest($"/v1/me/playlists", Method.GET);
            var request = await client.ExecuteAsync<PlaylistsDto>(postRequest).ConfigureAwait(false);
            if (request.Data is null)
            {
                return Array.Empty<PlaylistDto>();
            }
            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Request to '{request.ResponseUri}' end with code: {request.StatusCode} Reason: {request.Content}");
                return Array.Empty<PlaylistDto>();
            }
            return request.Data.items.Where(e => e.@public == publicOnly).Select(e => e);
        }

        public async Task<string> RequestForPlaylistsId(string playlistName)
        {
            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            var postRequest = new RestRequest($"/v1/me/playlists", Method.GET);
            var request = await client.ExecuteAsync<PlaylistsDto>(postRequest).ConfigureAwait(false);
            if (request.Data is null)
            {
                return string.Empty;
            }
            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Request to '{request.ResponseUri}' end with code: {request.StatusCode} Reason: {request.Content}");

                return string.Empty;
            }
            return request.Data.items.FirstOrDefault(e => e.name.Equals(playlistName, StringComparison.OrdinalIgnoreCase)).id;
        }

        public async Task<string> CreateCurrentUserPlaylist(string name, string description = null)
        {
            var userID = await RequestForUserId();
            if (userID is null)
            {
                return null;
            }

            return await CreatePlaylist(userID, name, description);
        }

        public async Task<string> CreatePlaylist(string userId, string name, string description = null)
        {
            logger.LogTrace($"Crate new Spotify playlist: '{name}'");

            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            var postRequest = new RestRequest($"/v1/users/{userId}/playlists", Method.POST);
            postRequest.AddHeader(contentType, "application/json");
            postRequest.AddJsonBody(new
            {
                name = name,
                description = description ?? string.Empty,
                @public = false
            });

            var request = await client.ExecuteAsync<PlaylistDto>(postRequest).ConfigureAwait(false);
            if (request.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError($"Create playlist '{name}' request end with code: {request.StatusCode} Reason: {request.Content}");
                return null;
            }

            return request.Data.id;
        }

        public async Task SetPlaylistCoverImage(string playlistId, string filePath)
        {
            if (!File.Exists(filePath))
            {
                logger.LogError($"Cover image to Spotify playlistId: '{playlistId}' will not be set. File not exists.");
                return;
            }

            logger.LogInformation($"Set cover image to Spotify playlistId: '{playlistId}'");

            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            var putRequest = new RestRequest($"v1/playlists/{playlistId}/images", Method.PUT);
            putRequest.AddHeader(contentType, "image/jpeg");

            var fileBytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            var fileBase64 = Convert.ToBase64String(fileBytes);
            putRequest.AddParameter("image/jpeg", fileBase64, ParameterType.RequestBody);

            var request = await client.ExecuteAsync(putRequest).ConfigureAwait(false);
            if (request.StatusCode != HttpStatusCode.Accepted)
            {
                logger.LogError($"Set cover image to Spotify playlistId: '{playlistId}' request end with code: {request.StatusCode} Reason: {request.Content}");
                return;
            }

            logger.LogInformation($"Set cover image to Spotify playlistId: '{playlistId}' completed");
        }

        public async Task SetPlaylistDescription(string playlistId, string description)
        {
            logger.LogInformation($"Set details to Spotify playlistId: '{playlistId}'");

            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            var putRequest = new RestRequest($"v1/playlists/{playlistId}", Method.PUT);
            putRequest.AddHeader(contentType, "application/json");
            putRequest.AddJsonBody(new
            {
                description = description ?? string.Empty,
            });

            var request = await client.ExecuteAsync(putRequest).ConfigureAwait(false);
            if (request.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError($"Set details to Spotify playlistId: '{playlistId}' request end with code: {request.StatusCode} Reason: {request.Content}");
                return;
            }

            logger.LogInformation($"Set details to Spotify playlistId: '{playlistId}' completed");
        }


        public async Task PopulatePlaylist(string id, IReadOnlyCollection<string> spotifyTrackIds)
        {
            logger.LogTrace($"Populate Spotify playlistId: '{id}'");

            if (spotifyTrackIds is null || !spotifyTrackIds.Any())
            {
                return;
            }

            var playlistTracks = await GetPlaylistTracks(id);

            if (playlistTracks is null)
            {
                return;
            }

            var tracksToAdd = new List<string>();
            foreach(var trackId in spotifyTrackIds)
            {
                if (playlistTracks.Any(playlistTrack => playlistTrack.uri == trackId))
                {
                    continue;
                }

                tracksToAdd.Add(trackId);
            }

            InsertToPlaylist(id, tracksToAdd);

            logger.LogTrace($"Spotify playlist: '{id}' update completed");
        }

        public async Task<IReadOnlyList<TrackItem>> GetPlaylistTracks(string playlistId)
        {
            List<TrackItem> collection = null;
            bool canExit = false;
            int offset = 0;
            int bucketSize = 100;
            do
            {
                var client = new RestClient(options.WebApi);
                client.Authenticator = new JwtAuthenticator(token.access_token);

                var getReqeust = new RestRequest($"/v1/playlists/{playlistId}/tracks", Method.GET);
                getReqeust.AddHeader(contentType, "application/json");
                getReqeust.AddParameter("offset", offset);
                getReqeust.AddParameter("limit", bucketSize);

                var request = await client.ExecuteAsync<PlaylistTrack2>(getReqeust).ConfigureAwait(false);
                if (request.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogError($"Get tracks of playlistId: '{playlistId}' request end with code: {request.StatusCode} Reason: {request.Content}");
                    return null;
                }

                if (request.Data is null)
                {
                    return null;
                }

                if (collection is null)
                {
                    collection = new List<TrackItem>(request.Data.total);
                }

                collection.AddRange(request.Data.items.Select(e => e.track));

                if (request.Data.total != collection.Count)
                {
                    offset = offset += bucketSize;
                }
                else
                {
                    canExit = true;
                }
            }
            while (!canExit);

            return collection;
        }

        public async Task<TrackItem> GetTrackInfo(string author, string title)
        {
            string albumTypeSingle = "single";

            var client = new RestClient(options.WebApi);
            client.Authenticator = new JwtAuthenticator(token.access_token);

            string artist = HttpUtility.UrlPathEncode(author);
            string trackSubstring = title.Length >= 5 ? title.Substring(0, 5) : title;
            string track = HttpUtility.UrlPathEncode(trackSubstring);
            var getRequest = new RestRequest($"/v1/search?q=artist%3A{artist}%20track%3A{track}&type=track", Method.GET);
            var request = await client.ExecuteAsync<TrackRoot>(getRequest).ConfigureAwait(false);
            if (request.Data is null)
            {
                return null;
            }
            if (request.StatusCode != System.Net.HttpStatusCode.OK)
            {
                logger.LogError($"Request to '{request.ResponseUri}' end with code: {request.StatusCode} Reason: {request.Content}");
                return null;
            }

            if (request.Data is null)
            {
                return null;
            }
            var trackItems = request.Data.tracks.items;

            TrackItem result = null;
            result = trackItems.FirstOrDefault(e => e.name == title);

            if (result is null)
            {
                result = trackItems.Where(e => e.name.Contains(title, StringComparison.OrdinalIgnoreCase)).OrderByDescending(e => e.popularity).FirstOrDefault();
            }

            if (result is null)
            {
                result = trackItems.Where(e => title.Contains(e.name, StringComparison.OrdinalIgnoreCase)).OrderByDescending(e => e.popularity).FirstOrDefault();
            }
            return result;
        }


        private void InsertToPlaylist(string playlistId, IEnumerable<string> trackIds)
        {
            logger.LogInformation($"Insert to playlistId: '{playlistId}' {trackIds.Count()} items");
            var batches = trackIds.Batch(100);

            int index = -1;
            foreach(var bucket in batches)
            {
                index++;
                var client = new RestClient(options.WebApi);
                client.Authenticator = new JwtAuthenticator(token.access_token);

                var postRequest = new RestRequest($"v1/playlists/{playlistId}/tracks", Method.POST);
                postRequest.AddHeader(contentType, "application/json");
                postRequest.AddJsonBody(new
                {
                    uris = bucket,
                    position = index * 100
                });


                var request = client.ExecuteAsync(postRequest).ConfigureAwait(false).GetAwaiter().GetResult();

                if (request.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    logger.LogError($"Refresh access token request end with code: {request.StatusCode} Reason: {request.Content}");
                    return;
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
