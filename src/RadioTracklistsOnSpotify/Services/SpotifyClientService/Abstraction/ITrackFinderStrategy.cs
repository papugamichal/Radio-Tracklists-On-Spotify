using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadioTracklistsOnSpotify.Services.SpotifyClientService.DTOs;

namespace RadioTracklistsOnSpotify.Services.SpotifyClientService.Abstraction
{
    public interface ITrackFinderStrategy
    {
        Task<TrackItem> Find(string artist, string title, Func<string, string, Task<IList<TrackItem>>> apiRequest);
    }
}
