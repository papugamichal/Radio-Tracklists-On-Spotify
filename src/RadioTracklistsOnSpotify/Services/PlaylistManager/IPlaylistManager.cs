using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioTracklistsOnSpotify.Services.PlaylistManager
{
    interface IPlaylistManager
    {
        Task PopulateSpotifyDailylist();
        Task PopulateSpotifyPlaylistForPeriod(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DateTime>> GetMissingSpotifyPlaylistsSince(DateTime date);
        Task PopulateTodayAndHandlePreviousPlaylists();
        Task LimitAccessToPlaylistOlderThan(TimeSpan limit);
    }
}
