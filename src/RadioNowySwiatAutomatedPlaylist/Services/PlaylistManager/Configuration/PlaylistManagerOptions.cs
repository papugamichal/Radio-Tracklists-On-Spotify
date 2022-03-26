namespace RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager.Configuration
{
    public class PlaylistManagerOptions
    {
        public bool IsPublic { get; set; } = true;
        public bool TodayPlaylistIsPublic { get; set; }
        public string TodayPlaylistName { get; set; }
        public string TodayPlaylistDescription { get; set; }
        public string TodayPlaylistCoverImagePath { get; set; }
        public bool YesterdaylistIsPublic { get; set; }
        public string YesterdayPlaylistName { get; set; }
        public string YesterdayPlaylistDescription { get; set; }
        public string YesterdayPlaylistCoverImagePath { get; set; }
        public bool DailyIsPublic { get; set; }
        public string DailyNamePrefix { get; set; }
        public string DailyCoverImagePath { get; set; }
        public string DailyDescription { get; set; }
    }
}
