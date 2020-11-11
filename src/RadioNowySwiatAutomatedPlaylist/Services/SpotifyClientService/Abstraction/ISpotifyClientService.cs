using RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.SpotifyClientService.Abstraction
{
    public interface ISpotifyClientService
    {
        Task SetAuthorizationCode(string code);
        bool IsAuthenticated();
        Uri GetAuthorizationCodeUrl();
        Task SetupAccessToken();
        Task<string> RequestForUserId();
        Task<string> CreatePlaylist(string userId, string name, string description = null);
        Task<string> CreateCurrentUserPlaylist(string name, string description = null);
        Task<IReadOnlyList<TrackItem>> GetPlaylistTracks(string playlistId);
        Task PopulatePlaylist(string id, IReadOnlyCollection<string> spotifyTrackIds);
        Task<TrackItem> GetTrackInfo(string author, string title);
        Task<IEnumerable<PlaylistDto>> RequestForUserPlaylists(bool publicOnly = false);
        Task SetPlaylistDescription(string playlistId, string description);
        Task SetPlaylistCoverImage(string playlistId, string filePath);
    }
}