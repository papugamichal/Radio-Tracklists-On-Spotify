namespace RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Configuration
{
    public class SpotifyClientOptions
    {
        public static string SectionName = "SpotifyClient";
        public string AccountWebApi { get; set; }
        public string WebApi { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string RedirectUrl { get; set; }
        public bool ShowLoginDialog { get; set; }
    }
}
