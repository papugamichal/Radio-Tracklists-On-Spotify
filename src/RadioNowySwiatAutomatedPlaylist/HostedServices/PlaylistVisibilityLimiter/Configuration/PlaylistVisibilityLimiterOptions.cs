using System;

namespace RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistVisibilityLimiter.Configuration
{
    public class PlaylistVisibilityLimiterOptions
    {
        public static string SectionName = "PlaylistVisibilityLimiter";
        public bool Enabled { get; set; }
        public int Limit { get; set; }
        public TimeSpan RefreshInterval { get; set; }
    }
}
