using Microsoft.Extensions.Logging;

namespace RadioTracklistsOnSpotify.Services.TrackCache
{
    public class FoundInSpotifyCache : TrackCache
    {
        public FoundInSpotifyCache(ILogger<FoundInSpotifyCache> logger) : base(logger, nameof(FoundInSpotifyCache))
        {
        }
    }

    public class NotFoundInSpotifyCache : TrackCache
    {
        public NotFoundInSpotifyCache(ILogger<NotFoundInSpotifyCache> logger) : base(logger, nameof(NotFoundInSpotifyCache))
        {
        }
    }
}
