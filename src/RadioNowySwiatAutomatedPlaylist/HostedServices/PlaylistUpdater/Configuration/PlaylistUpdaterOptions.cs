using System;

namespace RadioNowySwiatAutomatedPlaylist.HostedServices.PlaylistUpdater.Configuration
{
    public class PlaylistUpdaterOptions
    {
        public bool Enabled { get; set; }
        public TimeSpan RefreshInterval { get; set; }
    }
}
