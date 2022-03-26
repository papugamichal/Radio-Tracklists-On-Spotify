using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioTracklistsOnSpotify.Services.DataSourceService.Abstraction;
using RadioTracklistsOnSpotify.Services.PlaylistManager.Configuration;
using RadioTracklistsOnSpotify.Services.SpotifyClientService.Abstraction;
using RadioTracklistsOnSpotify.Services.TrackCache;

namespace RadioTracklistsOnSpotify.Services.PlaylistManager
{
    public sealed class RadioNowySwiatPlaylistManager : PlaylistManager
    {
        public RadioNowySwiatPlaylistManager(
            ILogger<RadioNowySwiatPlaylistManager> logger,
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
