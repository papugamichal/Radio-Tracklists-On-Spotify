using System.Collections.Generic;

namespace RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.DTOs
{
    public class PlaylistsDto
    {
        public string href { get; set; }
        public List<PlaylistDto> items { get; set; }
        public int limit { get; set; }
        public object next { get; set; }
        public int offset { get; set; }
        public object previous { get; set; }
        public int total { get; set; }
    }

    public class ExternalUrls
    {
        public string spotify { get; set; }
    }

    public class ExternalUrls2
    {
        public string spotify { get; set; }
    }

    public class Owner
    {
        public ExternalUrls2 external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
    }

    public class Tracks
    {
        public string href { get; set; }
        public int total { get; set; }
    }

    public class PlaylistDto
    {
        public bool collaborative { get; set; }
        public ExternalUrls external_urls { get; set; }
        public string href { get; set; }
        public string id { get; set; }
        public List<object> images { get; set; }
        public string name { get; set; }
        public Owner owner { get; set; }
        public bool @public { get; set; }
        public string snapshot_id { get; set; }
        public Tracks tracks { get; set; }
        public string type { get; set; }
        public string uri { get; set; }
        public string description { get; set; }
    }
}
