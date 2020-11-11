using System;

namespace RadioNowySwiatPlaylistBot.Services.DataSourceService
{
    public class TrackInfo
    {
        public TrackInfo(string artistName, string title, DateTime playTime)
        {
            ArtistName = artistName;
            Title = title;
            PlayTime = playTime;
        }

        public DateTime PlayTime { get; }
        public string ArtistName { get; }
        public string Title { get; }

    }
}
