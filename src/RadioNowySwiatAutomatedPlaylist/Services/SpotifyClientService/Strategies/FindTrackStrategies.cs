using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.DTOs;

namespace RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Strategies
{
    public class FullArtistFullTitleStrategy : ITrackFinderStrategy
    {
        public async Task<TrackItem> Find(string artist, string title, Func<string, string, Task<IList<TrackItem>>> apiRequest)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title) || apiRequest is null)
            {
                return null;
            }

            var result = await apiRequest(artist, title);

            if (result is null)
            {
                return null;
            }

            return result.First();
        }
    }

    public class FullArtistTitleWithoutApostropheStrategy : ITrackFinderStrategy
    {
        public async Task<TrackItem> Find(string artist, string title, Func<string, string, Task<IList<TrackItem>>> apiRequest)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title) || apiRequest is null)
            {
                return null;
            }

            title = title.Replace("'", string.Empty);

            var result = await apiRequest(artist, title);

            if (result is null)
            {
                return null;
            }

            return result.First();
        }
    }

    public class ArtistsWithoutCommaFullTitleStrategy : ITrackFinderStrategy
    {
        public async Task<TrackItem> Find(string artist, string title, Func<string, string, Task<IList<TrackItem>>> apiRequest)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title) || apiRequest is null)
            {
                return null;
            }

            artist = artist.Replace(",", string.Empty);

            var result = await apiRequest(artist, title);

            if (result is null)
            {
                return null;
            }

            return result.First();
        }
    }

    public class FullArtistsFirstFiveTitleCharactersStrategy : ITrackFinderStrategy
    {
        public async Task<TrackItem> Find(string artist, string title, Func<string, string, Task<IList<TrackItem>>> apiRequest)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title) || apiRequest is null)
            {
                return null;
            }

            title = title.Substring(0, 5);

            var result = await apiRequest(artist, title);

            if (result is null)
            {
                return null;
            }

            return result.First();
        }
    }
}
