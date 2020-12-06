using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction
{
    public interface ITrackFinderStrategy
    {
        Task<TrackItem> Find(string artist, string title, Func<string, string, Task<IList<TrackItem>>> apiRequest);
    }
}
