using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadioTracklistsOnSpotify.Services.DataSourceService.DTOs;

namespace RadioTracklistsOnSpotify.Services.DataSourceService.Abstraction
{
    public interface IDataSourceService
    {
        Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date);
        Task<Dictionary<DateTime, IReadOnlyCollection<TrackInfo>>> GetPlaylistForRange(DateTime startDate, DateTime endDate);
    }
}