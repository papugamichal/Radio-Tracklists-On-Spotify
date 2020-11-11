using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatPlaylistBot.Services.DataSourceService;
using RadioNowySwiatPlaylistBot.Services.DataSourceService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager.Configuration;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.TrackCache;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.PlaylistManager
{
    public class PlaylistManager : IPlaylistManager
    {
        private readonly ILogger<PlaylistManager> logger;
        private readonly PlaylistManagerOptions options;
        private readonly IDataSourceService dataSourceService;
        private readonly ISpotifyClientService spotifyClient;
        private readonly ITrackCache foundCache;
        private readonly ITrackCache notFoundCache;

        public PlaylistManager(
            ILogger<PlaylistManager> logger,
            IOptions<PlaylistManagerOptions> options,
            IDataSourceService dataSourceService,
            ISpotifyClientService spotifyClient,
            FoundInSpotifyCache spotofyCache,
            NotFoundInSpotifyCache notFoundCache)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
            this.dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            this.spotifyClient = spotifyClient ?? throw new ArgumentNullException(nameof(spotifyClient));
            this.foundCache = spotofyCache ?? throw new ArgumentNullException(nameof(spotofyCache));
            this.notFoundCache = notFoundCache ?? throw new ArgumentNullException(nameof(notFoundCache));
        }

        public Task<IEnumerable<DateTime>> GetMissingSpotifyPlaylistsSince(DateTime date)
        {
            var playlist = spotifyClient.RequestForUserPlaylists();


            throw new NotImplementedException();
        }

        public async Task PopulateSpotifyDailylist()
        {
            if (!IsAuthenticated())
            {
                logger.LogInformation("User is not authenticated");
                return;
            }

            var radioPlaylist = await GetTodayRadioPlaylist().ConfigureAwait(false); ;

            if (radioPlaylist is null)
            {
                return;
            }

            var spotifyPlaylistId = await EnsureSpotifyDailyPlaylist().ConfigureAwait(false); ;

            if (spotifyPlaylistId is null)
            {
                return;
            }

            var orderedRadioPlaylist = EnsureRadioPlaylistItemsOrder(radioPlaylist);

            var radioPlaylistAsSpotifyTrackId = await ConvertToSpotifyTrackIds(orderedRadioPlaylist);
            
            await PopulateSpotifyPlaylist(spotifyPlaylistId, radioPlaylistAsSpotifyTrackId).ConfigureAwait(false);
            await UpdatePlaylistDescription(spotifyPlaylistId, orderedRadioPlaylist.Count(), radioPlaylistAsSpotifyTrackId.Count());
        }

        private async Task PopulateSpotifyPlaylist(string spotifyPlaylistId, IEnumerable<string> radioPlaylistAsSpotifyTrackId)
        {
            await spotifyClient.PopulatePlaylist(spotifyPlaylistId, radioPlaylistAsSpotifyTrackId.ToArray()).ConfigureAwait(false);
        }

        private async Task UpdatePlaylistDescription(string playlistId, int radioPlaylistLenght, int availableSpotifyTrackCount)
        {
            string updatedDescription = options.DailyDescription + $" Dopasowano {availableSpotifyTrackCount}/{radioPlaylistLenght} utwory.";
            await spotifyClient.SetPlaylistDescription(playlistId, updatedDescription);
        }

        private async Task<IEnumerable<string>> ConvertToSpotifyTrackIds(IEnumerable<(string ArtistName, string Title)> playlist)
        {
            var collection = new List<string>(playlist.Count());

            foreach (var trackInfo in playlist)
            {
                var isInCache = notFoundCache.InCache(trackInfo.ArtistName, trackInfo.Title);

                if (isInCache)
                {
                    // this track was not found in Spotify library, do not check it again
                    continue;
                }
                
                var cached = foundCache.GetFromCache(trackInfo.ArtistName, trackInfo.Title);

                if (cached is {})
                {
                    // this track was found in Spotfy library, get cached
                    collection.Add(cached);
                    continue;
                }

                var spotifyTrackInfo =  await spotifyClient.GetTrackInfo(trackInfo.ArtistName, trackInfo.Title);

                if (spotifyTrackInfo is null)
                {
                    logger.LogWarning($"Unable to find this beat in Spotify - artis: '{trackInfo.ArtistName}' title: '{trackInfo.Title}'");
                    notFoundCache.AddToCache(trackInfo.ArtistName, trackInfo.Title, string.Empty);
                    continue;
                }

                foundCache.AddToCache(trackInfo.ArtistName, trackInfo.Title, spotifyTrackInfo.uri);
                collection.Add(spotifyTrackInfo.uri);
            }
            logger.LogInformation($"Found {collection.Count}/{playlist.Count()} tracks");

            return collection;
        }

        private IEnumerable<(string ArtistName, string Title)> EnsureRadioPlaylistItemsOrder(IReadOnlyList<TrackInfo> radioPlaylist) 
            => radioPlaylist.OrderByDescending(trackInfo => trackInfo.PlayTime).Select(trackInfo => (trackInfo.ArtistName, trackInfo.Title));

        private bool IsAuthenticated() 
            => spotifyClient.IsAuthenticated();

        public Task PopulateSpotifyPlaylistForPeriod(DateTime startDate, DateTime endDate)
        {
            if (!IsAuthenticated())
            {
                return Task.CompletedTask;
            }

            throw new NotImplementedException();
        }

        private async Task<IReadOnlyList<TrackInfo>> GetTodayRadioPlaylist()
        {
            if (!IsAuthenticated())
            {
                return null;
            }

            return await dataSourceService.GetPlaylistFor(DateTime.Today).ConfigureAwait(false); ;
        }

        private async Task<string> EnsureSpotifyDailyPlaylist()
        {
            var userPlaylists = await spotifyClient.RequestForUserPlaylists().ConfigureAwait(false); ;
            var dailyPlaylistName = GetSpotifyDailyPlaylistName(DateTime.Today);

            var dailyPlaylist = userPlaylists.FirstOrDefault(playlist => playlist.name.Equals(dailyPlaylistName, StringComparison.OrdinalIgnoreCase));

            string playlistId;
            if (dailyPlaylist is null)
            {
                playlistId = await spotifyClient.CreateCurrentUserPlaylist(dailyPlaylistName, options.IsPublic, options.DailyDescription).ConfigureAwait(false);
                await spotifyClient.SetPlaylistCoverImage(playlistId, options.DailyCoverImagePath);
            }
            else
            {
                playlistId = dailyPlaylist.id;
            }

            return playlistId;
        }

        private string GetSpotifyDailyPlaylistName(DateTime date)
        {
            return string.Format("{0} {1}", options.DailyNamePrefix, date.ToString("yyyy.MM.dd"));
        }
    }
}
