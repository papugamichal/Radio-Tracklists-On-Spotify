using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using RestSharp;
using RestSharp.Authenticators;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Strategies;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Configuration;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs.PlaylistTrack2;

namespace RadioNowySwiatPlaylistBot.Services.SpotifyClientService
{
    public class SpotifyClientService : ISpotifyClientService
    {
        private readonly ILogger<SpotifyClientService> logger;
        private readonly IOptions<SpotifyClientOptions> iOptions;
        private readonly ISpotifyAuthorizationService authorizationService;
        private SpotifyClientOptions options => iOptions.Value;

        private const string contentType = "content-type";

        private readonly IReadOnlyList<ITrackFinderStrategy> trackSearchStrategies;

        public SpotifyClientService(
            ILogger<SpotifyClientService> logger,
            IOptions<SpotifyClientOptions> options,
            ISpotifyAuthorizationService authorizationService
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.iOptions = options ?? throw new ArgumentNullException(nameof(options));
            this.authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            this.trackSearchStrategies = new List<ITrackFinderStrategy>()
            {
                new FullArtistFullTitleStrategy(),
                new FullArtistTitleWithoutApostropheStrategy(),
                new FullArtistsFirstFiveTitleCharactersStrategy(),
                new ArtistsWithoutCommaFullTitleStrategy(),
            };
        }

        /* Authorization */
        public Uri GetAuthorizationUri()
        {
            return this.authorizationService.GetAuthorizationCodeUri();
        }

        public bool IsAuthenticated()
        {
            return this.authorizationService.IsAuthenticated();
        }

        /* API */
        public async Task<string> RequestForUserId()
        {
            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

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

        public async Task<IEnumerable<PlaylistDto>> RequestForUserPlaylists()
        {
            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

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
            return request.Data.items.Select(e => e);
        }

        public async Task<IEnumerable<PlaylistDto>> RequestPlaylistTracks(string playlistId)
        {
            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

            var postRequest = new RestRequest($"/v1/playlists/{playlistId}/tracks", Method.GET);
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
            return request.Data.items.Select(e => e);
        }

        public async Task<string> RequestForPlaylistsId(string playlistName)
        {
            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

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

        public async Task<string> CreateCurrentUserPlaylist(string name, bool @public = false, string description = null)
        {
            var userID = await RequestForUserId();
            if (userID is null)
            {
                return null;
            }

            return await CreatePlaylist(userID, name, @public, description);
        }

        public async Task<string> CreatePlaylist(string userId, string name, bool @public = false, string description = null)
        {
            logger.LogTrace($"Crate new Spotify playlist: '{name}'");

            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

            var postRequest = new RestRequest($"/v1/users/{userId}/playlists", Method.POST);
            postRequest.AddHeader(contentType, "application/json");
            postRequest.AddJsonBody(new
            {
                name = name,
                description = description ?? string.Empty,
                @public = @public
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
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

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
            logger.LogInformation($"Set Spotify playlistId: '{playlistId}' details");

            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

            var putRequest = new RestRequest($"v1/playlists/{playlistId}", Method.PUT);
            putRequest.AddHeader(contentType, "application/json");
            putRequest.AddJsonBody(new
            {
                description = description ?? string.Empty,
            });

            var request = await client.ExecuteAsync(putRequest).ConfigureAwait(false);
            if (request.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError($"Set Spotify playlistId: '{playlistId}' details request end with code: {request.StatusCode} Reason: {request.Content}");
                return;
            }

            logger.LogInformation($"Set Spotify playlistId: '{playlistId}' details completed");
        }

        public Task MakePlaylistPublic(string playlistId)
        {
            return UpdatePlaylistVisibility(playlistId, true);
        }

        public Task MakePlaylistPrivate(string playlistId)
        {
            return UpdatePlaylistVisibility(playlistId, false);
        }

        private async Task UpdatePlaylistVisibility(string playlistId, bool isPublic)
        {
            logger.LogInformation($"Update Spotify playlist: '{playlistId}' visibility - [isPublic: {isPublic.ToString()}]");

            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

            var putRequest = new RestRequest($"v1/playlists/{playlistId}", Method.PUT);
            putRequest.AddHeader(contentType, "application/json");
            putRequest.AddJsonBody(new
            {
                @public = isPublic ? true : false,
            });
            
            var request = await client.ExecuteAsync(putRequest).ConfigureAwait(false);
            if (request.StatusCode != HttpStatusCode.Accepted)
            {
                logger.LogError($"Update Spotify playlist: '{playlistId}' visibility request end with code: {request.StatusCode} Reason: {request.Content}");
                return;
            }

            logger.LogInformation($"Update Spotify playlist: '{playlistId}' visibility request completed");
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

        public async Task ClearPlaylist(string playlistId)
        {
            logger.LogTrace($"Clear Spotify playlistId: '{playlistId}'");

            var playlistTracks = await GetPlaylistTracks(playlistId);
            if (playlistTracks is null)
            {
                return;
            }

            var batches = playlistTracks.Batch(100);

            int index = -1;
            foreach (var bucket in batches)
            {
                index++;
                var client = new RestClient(options.WebApi);
                var accessToken = authorizationService.GetToken();
                client.Authenticator = new JwtAuthenticator(accessToken);

                var deleteRequest = new RestRequest($"/v1/playlists/{playlistId}/tracks", Method.DELETE);
                deleteRequest.AddHeader(contentType, "application/json");
                deleteRequest.AddJsonBody(new
                {
                    tracks = bucket.Select(e => new { uri = e.uri }).ToArray()
                });

                var request = await client.ExecuteAsync<int>(deleteRequest).ConfigureAwait(false);
                if (request.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogError($"Delete all tracks of playlistId: '{playlistId}' request end with code: {request.StatusCode} Reason: {request.Content}");
                    return;
                }
            }

            playlistTracks = await GetPlaylistTracks(playlistId).ConfigureAwait(false);
            if (playlistTracks is null)
            {
                return;
            }

            if (playlistTracks.Any())
            {
                throw new Exception($"This method should remove all track in playlist '{playlistId}' but ther are some left! ({playlistTracks.Count})");
            }

            logger.LogTrace($"Spotify playlist: '{playlistId}' update completed");
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
                var accessToken = authorizationService.GetToken();
                client.Authenticator = new JwtAuthenticator(accessToken);

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
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

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

        public async Task<TrackItem> GetTrackInfo_v2(string author, string title)
        {
            TrackItem result = null;
            foreach (var strategy in trackSearchStrategies)
            {
                var finding = await strategy.Find(author, title, RequestForTrackInfo);

                if (finding is null)
                {
                    continue;
                }

                result = finding;
                break;
            }

            return result;
        }

        private async Task<IList<TrackItem>> RequestForTrackInfo(string author, string title)
        {
            var client = new RestClient(options.WebApi);
            var accessToken = authorizationService.GetToken();
            client.Authenticator = new JwtAuthenticator(accessToken);

            string artist = HttpUtility.UrlPathEncode(author);
            string track = HttpUtility.UrlPathEncode(title);
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

            return request.Data.tracks.items;
        }

        private void InsertToPlaylist(string playlistId, IEnumerable<string> trackIds)
        {
            var batches = trackIds.Batch(100);

            int index = -1;
            foreach(var bucket in batches)
            {
                index++;
                var client = new RestClient(options.WebApi);
                var accessToken = authorizationService.GetToken();
                client.Authenticator = new JwtAuthenticator(accessToken);

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
            logger.LogInformation($"Inserted to playlistId: '{playlistId}' {trackIds.Count()} items");
        }

        public Task SetupAccessToken(string authorizationCode)
        {
            return Task.FromResult(this.authorizationService.SetupAccessToken(authorizationCode));
        }
    }
}


