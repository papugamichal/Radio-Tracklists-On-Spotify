using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatPlaylistBot.Services.DataSourceService;
using RadioNowySwiatPlaylistBot.Services.DataSourceService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.PlaylistManager.Configuration;
using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatPlaylistBot.Services.TrackCache;

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
            throw new NotImplementedException();
        }

        /* Version 1 */
        public async Task PopulateSpotifyDailylist()
        {
            if (!IsAuthenticated())
            {
                logger.LogInformation("User is not authenticated");
                return;
            }

            var radioPlaylist = await GetTodayRadioPlaylistFromWeb().ConfigureAwait(false); ;

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
            await UpdatePlaylistDescription(spotifyPlaylistId, orderedRadioPlaylist.Count(), radioPlaylistAsSpotifyTrackId.Count(), options.DailyDescription);
        }

        /* Version 2 */
        public async Task PopulateTodayAndHandlePreviousPlaylists()
        {
            if (!IsAuthenticated())
            {
                logger.LogInformation("User is not authenticated");
                return;
            }

            var radioPlaylist = await GetTodayRadioPlaylistFromWeb().ConfigureAwait(false);
            if (radioPlaylist is null)
            {
                return;
            }

            #region Ensure Today, Yesterday and Daily playlists exists

            var orderedRadioPlaylist = EnsureRadioPlaylistItemsOrder(radioPlaylist);
            var radioPlaylistAsSpotifyTrackId = await ConvertToSpotifyTrackIds(orderedRadioPlaylist);

            var todayPlaylistId = await EnsureTodaySpotifyPlaylistExists(options.TodayPlaylistIsPublic).ConfigureAwait(false);
            if (todayPlaylistId is null)
            {
                return;
            }

            var yesterdayPlaylistId = await EnsureYesterdaySpotifyPlaylistExists(options.YesterdaylistIsPublic).ConfigureAwait(false);
            if (yesterdayPlaylistId is null)
            {
                return;
            }

            var dailyPlaylistName = GetSpotifyDailyPlaylistName(DateTime.Today);
            var dailyPlaylistId = await EnsureSpotifyPlaylistExists(dailyPlaylistName, options.DailyDescription, options.DailyIsPublic, options.DailyCoverImagePath).ConfigureAwait(false);
            if (dailyPlaylistId is null)
            {
                return;
            }

            #endregion

            #region Update Yesterday and clead Today playlist if Daily playlist is empty (its new day created)

            var dailyPlaylistTrackCount = await GetPlaylistsTrackCount(dailyPlaylistId).ConfigureAwait(false);
            if (dailyPlaylistTrackCount == 0)
            {
                await ClearPlaylist(yesterdayPlaylistId).ConfigureAwait(false);
                await PopulateYesterdayTracksToPlaylist(yesterdayPlaylistId).ConfigureAwait(false);
                await ClearPlaylist(todayPlaylistId).ConfigureAwait(false);
                await MakeDayBeforeYesterdayPlaylistPublic();
            }

            #endregion

            #region Populate Today and Daily playlist

            await PopulateSpotifyPlaylist(todayPlaylistId, radioPlaylistAsSpotifyTrackId).ConfigureAwait(false);
            await UpdatePlaylistDescription(todayPlaylistId, orderedRadioPlaylist.Count(), radioPlaylistAsSpotifyTrackId.Count(), options.TodayPlaylistDescription);

            await PopulateSpotifyPlaylist(dailyPlaylistId, radioPlaylistAsSpotifyTrackId).ConfigureAwait(false);
            await UpdatePlaylistDescription(dailyPlaylistId, orderedRadioPlaylist.Count(), radioPlaylistAsSpotifyTrackId.Count(), options.DailyDescription);

            #endregion
        }

        private async Task MakeDayBeforeYesterdayPlaylistPublic()
        {
            var userPlaylist = await this.spotifyClient.RequestForUserPlaylists().ConfigureAwait(false);
            if (userPlaylist is null)
            {
                return;
            }

            var dayBeforeYesterdayPlaylistName = GetSpotifyDailyPlaylistName(DateTime.Today.AddDays(-2));

            var dayBeforeYesterdayPlaylist = userPlaylist.Where(playlist => playlist.name.Equals(dayBeforeYesterdayPlaylistName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (dayBeforeYesterdayPlaylist is null)
            {
                // log here warning!
                return;
            }

            await this.spotifyClient.MakePlaylistPublic(dayBeforeYesterdayPlaylist.id).ConfigureAwait(false);
        }

        private async Task PopulateYesterdayTracksToPlaylist(string playlistId)
        {
            var userPlaylist = await this.spotifyClient.RequestForUserPlaylists().ConfigureAwait(false);
            if (userPlaylist is null)
            {
                return;
            }

            var yesterdayDailyPlaylistName = GetSpotifyDailyPlaylistName(DateTime.Today.AddDays(-1));

            var yesterdayDailyPlaylist = userPlaylist.Where(playlist => playlist.name.Equals(yesterdayDailyPlaylistName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (yesterdayDailyPlaylist is null)
            {
                // log here warning!
                return;
            }

            var tracks = await this.spotifyClient.GetPlaylistTracks(yesterdayDailyPlaylist.id);
            if (tracks is null)
            {
                return;
            }

            var trackIds = tracks.Select(e => e.uri).ToList();
            await this.spotifyClient.PopulatePlaylist(playlistId, trackIds).ConfigureAwait(false);
            var description = System.Web.HttpUtility.HtmlDecode(yesterdayDailyPlaylist.description);
            await this.spotifyClient.SetPlaylistDescription(playlistId, description).ConfigureAwait(false);
        }

        private Task ClearPlaylist(string playlistId)
        {
            return spotifyClient.ClearPlaylist(playlistId);
        }

        private Task PopulateSpotifyPlaylist(string spotifyPlaylistId, IEnumerable<string> radioPlaylistAsSpotifyTrackId)
        {
            return spotifyClient.PopulatePlaylist(spotifyPlaylistId, radioPlaylistAsSpotifyTrackId.ToArray());
        }

        private Task UpdatePlaylistDescription(string playlistId, int radioPlaylistLenght, int availableSpotifyTrackCount, string summary)
        {
            string updatedDescription = $"{summary} Dopasowano {availableSpotifyTrackCount}/{radioPlaylistLenght} utwory.";
            return spotifyClient.SetPlaylistDescription(playlistId, updatedDescription);
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

                if (cached is { })
                {
                    // this track was found in Spotfy library, get cached
                    collection.Add(cached);
                    continue;
                }

                var spotifyTrackInfo = await spotifyClient.GetTrackInfo(trackInfo.ArtistName, trackInfo.Title);

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

        private async Task<IReadOnlyList<TrackInfo>> GetTodayRadioPlaylistFromWeb()
        {
            if (!IsAuthenticated())
            {
                return null;
            }

            return await dataSourceService.GetPlaylistFor(DateTime.Today).ConfigureAwait(false); ;
        }

        /* Version 1 */
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

        public Task<string> EnsureTodaySpotifyPlaylistExists(bool isPublic = true)
        {
            return EnsureSpotifyPlaylistExists(options.TodayPlaylistName, options.TodayPlaylistDescription, isPublic, options.TodayPlaylistCoverImagePath);
        }

        public Task<string> EnsureYesterdaySpotifyPlaylistExists(bool isPublic = true)
        {
            return EnsureSpotifyPlaylistExists(options.YesterdayPlaylistName, options.YesterdayPlaylistDescription, isPublic, options.YesterdayPlaylistCoverImagePath);
        }

        private async Task<string> EnsureSpotifyPlaylistExists(string name, string description, bool isPublic, string coverImagePath)
        {
            var userPlaylists = await spotifyClient.RequestForUserPlaylists().ConfigureAwait(false);

            var lookedPlaylist = userPlaylists.FirstOrDefault(playlist => playlist.name.Equals(name, StringComparison.OrdinalIgnoreCase));

            string playlistId;
            if (lookedPlaylist is null)
            {
                playlistId = await spotifyClient.CreateCurrentUserPlaylist(name, isPublic, description).ConfigureAwait(false);

                if (File.Exists(coverImagePath))
                {
                    await spotifyClient.SetPlaylistCoverImage(playlistId, options.DailyCoverImagePath);
                }
            }
            else
            {
                playlistId = lookedPlaylist.id;
            }

            return playlistId;
        }

        private async Task<int> GetPlaylistsTrackCount(string playlistId)
        {
            var tracks = await this.spotifyClient.GetPlaylistTracks(playlistId).ConfigureAwait(false);
            
            if (tracks is null)
            {
                return default;
            }

            return tracks.Count();
        }

        private string GetSpotifyDailyPlaylistName(DateTime date)
        {
            return string.Format("{0} {1}", options.DailyNamePrefix, date.ToString("yyyy.MM.dd"));
        }
    }
}
