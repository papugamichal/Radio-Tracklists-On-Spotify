using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.PlaylistManager.Configuration
{
    public class PlaylistManagerOptions
    {
        public static string SectionName = "PlaylistManager";
        public bool IsPublic { get; set; }
        public string DailyNamePrefix { get; set; }
        public string DailyCoverImagePath { get; set; }
        public string DailyDescription { get; set; }
    }
}
