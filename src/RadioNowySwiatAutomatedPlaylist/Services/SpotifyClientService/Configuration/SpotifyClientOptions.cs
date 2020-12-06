using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Security;

namespace RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Configuration
{
    public class SpotifyClientOptions
    {
        public static string SectionName = "SpotifyClient";
        public string WebApi { get; set; }
    }
}
