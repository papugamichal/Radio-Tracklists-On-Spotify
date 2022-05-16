using System;

namespace RadioTracklistsOnSpotify.Services.DataSourceService.DTOs
{
    public class TrackInfo : IEquatable<TrackInfo>
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

        public bool Equals(TrackInfo other)
        {
            if (ReferenceEquals(null, other)) return false; 
            if (ReferenceEquals(this, other)) return true;

            return ArtistName == other.ArtistName &&
                Title == other.Title &&
                PlayTime == other.PlayTime;
        }
    }
}
