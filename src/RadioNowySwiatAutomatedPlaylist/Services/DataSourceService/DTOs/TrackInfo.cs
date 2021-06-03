using System;

namespace RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.DTOs
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
