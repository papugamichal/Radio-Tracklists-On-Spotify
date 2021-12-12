using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Abstraction;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.Configuration;
using RadioNowySwiatAutomatedPlaylist.Services.DataSourceService.DTOs;

namespace RadioNowySwiatAutomatedPlaylist.Services.DataSourceService
{
    public class Radio357DataSourceService : IDataSourceService
    {
        private readonly ILogger<Radio357DataSourceService> logger;
        private readonly DataSourceOptions options;

        private const string TracksListHtmlCollectionXPath = "/html/body/div[1]/section/div/div/div[4]/div/table/tbody";
        private const string RemoveFromSongTitle = "(Polski Top Radia 357)";

        public Radio357DataSourceService(
            ILogger<Radio357DataSourceService> logger,
            IOptions<DataSourceOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        }

        public async Task<Dictionary<DateTime, IReadOnlyCollection<TrackInfo>>> GetPlaylistForRange(DateTime startDate, DateTime endDate)
        {
            var dateRange = Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days)
                .Select(offset => endDate.AddDays(offset * -1))
                .ToList();

            var result = new Dictionary<DateTime, IReadOnlyCollection<TrackInfo>>();
            foreach (var dateOfInterest in dateRange)
            {
                var datePlaylist = await GetPlaylistFor(dateOfInterest);

                if (datePlaylist is null)
                {
                    continue;
                }
                result.TryAdd(dateOfInterest, datePlaylist);
            }

            return result;
        }

        public async Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            if (date == tomorrow) return Array.Empty<TrackInfo>();

            var tasks = new[] { GetPlaylistFor(date, 0, 10), GetPlaylistFor(date, 10, 20), GetPlaylistFor(date, 20, 0) };
            var requestResults = await Task.WhenAll(tasks);
            return requestResults.SelectMany(tracks => tracks).OrderBy(track => track.PlayTime).ToList();
        }

        private async Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date, int startHour, int endHour)
        {
            var url = GetDataSourceQueryUrlFor(date, startHour, endHour);
            var trackHtmlBoxes = await GetDataSourceHtmlElementCollection(url);
            if(trackHtmlBoxes is null)
            {
                return Array.Empty<TrackInfo>();
            }

            return RetriveTracksInfoFrom(trackHtmlBoxes);
        }

        private string GetDataSourceQueryUrlFor(DateTime date, int startHour, int endHour)
        {
            var timeRange = $"&time_from={startHour}&time_to={endHour}";
            return options.PlaylistEndpoint + date.ToString(options.DateFormat) + timeRange;
        }

        private async Task<IEnumerable<HtmlNode>> GetDataSourceHtmlElementCollection(string url)
        {
            var htmlDocument = await GetRawContent(url);
            if (htmlDocument is null)
            {
                return null;
            }

            var playListNode = htmlDocument.DocumentNode.SelectSingleNode(TracksListHtmlCollectionXPath);
            return playListNode.ChildNodes;
        }

        private static List<TrackInfo> RetriveTracksInfoFrom(IEnumerable<HtmlNode> htmlNodes)
        {
            var outputCollectionSize = htmlNodes.Count() / 2;
            var collection = new List<TrackInfo>(outputCollectionSize);
            foreach (var node in htmlNodes)
            {
                if (!node.ChildNodes.Any() || node.ChildNodes.Count <= 3) continue;
                
                var playTime = TimeSpan.Parse(node.ChildNodes[1].InnerHtml);
                var playDateTime = new DateTime().Add(playTime);

                var _artistTitle = node.ChildNodes[3].ChildNodes[1].InnerText?.Trim();

                var splitted = _artistTitle.Split('-');
                var artis = splitted?.First();
                var title = splitted?.Last()?.Replace(RemoveFromSongTitle, string.Empty);

                var item = new TrackInfo(artis, title, playDateTime);
                collection.Add(item);
            }
            return collection;
        }

        private async Task<HtmlDocument> GetRawContent(string url)
        {
            HtmlDocument result = null;
            var sw = new Stopwatch();
            try
            {
                var web = new HtmlWeb();
                sw.Start();
                result = await web.LoadFromWebAsync(url);
                logger.LogInformation($"Performance monitor. Load HTML document from '{url}' in: {sw.ElapsedMilliseconds} ms");

                if (result is null)
                {
                    logger.LogError($"Load HTML document from '{url}' return null");
                    return result;
                }

                if (web.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogError($"Request to load HTML document from '{url}' end with code: '{web.StatusCode}'");
                    return result;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Unexpected error occured during fetch HTML document from '{url}'!");
            }

            return result;
        }
    }
}
