using RadioNowySwiatPlaylistBot.Services.DataSourceService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadioNowySwiatPlaylistBot.Services.DataSourceService.Abstraction
{
    public interface IDataSourceService
    {
        Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date);
        Task<Dictionary<DateTime, IReadOnlyCollection<TrackInfo>>> GetPlaylistForRange(DateTime startDate, DateTime endDate);
    }
}