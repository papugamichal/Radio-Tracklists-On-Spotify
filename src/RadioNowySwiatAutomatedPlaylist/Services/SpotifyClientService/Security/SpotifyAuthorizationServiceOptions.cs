namespace RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Security
{
    public class SpotifyAuthorizationServiceOptions
    {
        public static string SectionName = "SpotifyAuthorization";
        public string AccountWebApi { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string RedirectUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public bool ShowLoginDialog { get; set; }
    }
}
