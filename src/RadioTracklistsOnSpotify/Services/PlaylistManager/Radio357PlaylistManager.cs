using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioTracklistsOnSpotify.Services.DataSourceService.Abstraction;
using RadioTracklistsOnSpotify.Services.PlaylistManager.Configuration;
using RadioTracklistsOnSpotify.Services.SpotifyClientService.Abstraction;
using RadioTracklistsOnSpotify.Services.TrackCache;

namespace RadioTracklistsOnSpotify.Services.PlaylistManager
{
    public sealed class Radio357PlaylistManager : PlaylistManager
    {
        public Radio357PlaylistManager(
            ILogger<Radio357PlaylistManager> logger,
            IOptions<PlaylistManagerOptions> options,
            IDataSourceService dataSourceService,
            ISpotifyClientService spotifyClient,
            FoundInSpotifyCache spotofyCache,
            NotFoundInSpotifyCache notFoundCache)
            : base(logger, options, dataSourceService, spotifyClient, spotofyCache, notFoundCache)
        {
        }
    }
}
