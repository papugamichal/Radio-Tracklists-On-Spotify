namespace RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Configuration
{
    public class DataSourceOptions
    {
        public static string SectionName = "DataSource";

        public string PlaylistEndpoint { get; set; }
        public string DateFormat { get; set; }
    }
}
