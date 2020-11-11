using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.TrackCache
{
    public interface ITrackCache
    {
        void AddToCache(string artist, string beat, string spotifyTrackUri);
        bool InCache(string artist, string beat);
        string GetFromCache(string artis, string beat);
    }
}
