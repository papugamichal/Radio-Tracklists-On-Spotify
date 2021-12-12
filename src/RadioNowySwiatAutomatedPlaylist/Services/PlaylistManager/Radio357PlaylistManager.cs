using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.SpotifyClientService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.TrackCache;

namespace RadioNowySwiatAutomatedPlaylist.Services.PlaylistManager
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
