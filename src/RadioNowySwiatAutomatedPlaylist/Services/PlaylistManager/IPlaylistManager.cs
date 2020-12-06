using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.PlaylistManager
{
    interface IPlaylistManager
    {
        Task PopulateSpotifyDailylist();
        Task PopulateSpotifyPlaylistForPeriod(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DateTime>> GetMissingSpotifyPlaylistsSince(DateTime date);
        Task PopulateTodayAndHandlePreviousPlaylists();
    }
}
