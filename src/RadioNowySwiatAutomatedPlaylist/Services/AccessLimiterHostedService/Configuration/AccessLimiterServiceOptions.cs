using System;

namespace RadioNowySwiatPlaylistBot.Services.AccessLimiterHostedService.Configuration
{
    public class AccessLimiterServiceOptions
    {
        public static string SectionName = "AccessLimiterService";
        public bool Enabled { get; set; }
        public int PublicAccessPlaylistLimit { get; set; }
        public TimeSpan RefreshInterval { get; set; }
    }
}
