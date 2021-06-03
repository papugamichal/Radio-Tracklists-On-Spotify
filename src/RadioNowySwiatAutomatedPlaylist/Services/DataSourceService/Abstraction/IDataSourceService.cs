using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.DTOs;

namespace RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Abstraction
{
    public interface IDataSourceService
    {
        Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date);
        Task<Dictionary<DateTime, IReadOnlyCollection<TrackInfo>>> GetPlaylistForRange(DateTime startDate, DateTime endDate);
    }
}