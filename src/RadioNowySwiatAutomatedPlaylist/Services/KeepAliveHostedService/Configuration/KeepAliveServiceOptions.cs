using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.DailyPlaylistHostedService.Configuration
{
    public class KeepAliveServiceOptions
    {
        public static string SectionName = "KeepAliveService";
        public bool Enabled { get; set; }
        public TimeSpan RefreshInterval { get; set; }
        public string Url { get; set; }
    }
}
