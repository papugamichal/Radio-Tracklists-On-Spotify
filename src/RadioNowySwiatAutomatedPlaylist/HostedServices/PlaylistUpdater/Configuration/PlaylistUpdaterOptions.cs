using System;

namespace RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater.Configuration
{
    public class PlaylistUpdaterOptions
    {
        public static string SectionName = "DailyPlaylistService";

        public TimeSpan RefreshInterval { get; set; }
    }
}
