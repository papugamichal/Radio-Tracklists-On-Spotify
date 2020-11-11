using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.DailyPlaylistHostedService.Configuration
{
    public class DailyPlaylistServiceOptions
    {
        public static string SectionName = "DailyPlaylistService";

        public TimeSpan RefreshInterval { get; set; }
    }
}
