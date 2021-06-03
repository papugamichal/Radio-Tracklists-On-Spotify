using System;

namespace RadioNowySwiatAutomatedPlaylist.HostedServices.KeepAlive.Configuration
{
    public class KeepAliveServiceOptions
    {
        public static string SectionName = "KeepAliveService";
        public bool Enabled { get; set; }
        public TimeSpan RefreshInterval { get; set; }
        public string Url { get; set; }
    }
}
