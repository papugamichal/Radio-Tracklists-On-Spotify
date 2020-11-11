using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.TrackCache
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
