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
    public class RadioNowySwiatDataSourceService : IDataSourceService
    {
        private readonly ILogger<RadioNowySwiatDataSourceService> logger;
        private readonly DataSourceOptions options;

        private const string TrackHtmlClassName = "lista-ogolna-box";
        private const string TracksListDivClass = "proradio-the_content";

        public RadioNowySwiatDataSourceService(
            ILogger<RadioNowySwiatDataSourceService> logger,
            IOptions<DataSourceOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        }

        public async Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date)
        {
            var url = GetDataSourceUrlFor(date);
            var trackHtmlBoxes = await GetDataSourceHtmlElementCollection(url).ConfigureAwait(false);
            var trackCollection = RetriveTracksInfoFrom(trackHtmlBoxes);
            return trackCollection.ToList();
        }

        private static IEnumerable<TrackInfo> RetriveTracksInfoFrom(IEnumerable<HtmlNode> htmlNodes)
        {
            if (htmlNodes is null)
            {
                return Array.Empty<TrackInfo>();
            }

            var collection = new List<TrackInfo>(htmlNodes.Count());
            foreach (var node in htmlNodes)
            {
                string title = node.ChildNodes[1].ChildNodes[0].InnerHtml;
                string artis = node.ChildNodes[1].ChildNodes[1].InnerHtml;

                if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(artis)) continue;
                var playTime = TimeSpan.Parse(node.ChildNodes[0].InnerHtml);
                var playDateTime = new DateTime().Add(playTime);

                var item = new TrackInfo(artis, title, playDateTime);
                collection.Add(item);
            }
            return collection;
        }

        private string GetDataSourceUrlFor(DateTime date)
        {
            return options.PlaylistEndpoint + date.ToString(options.DateFormat);
        }

        private async Task<IEnumerable<HtmlNode>> GetDataSourceHtmlElementCollection(string url)
        {
            var htmlDocument = await GetRawContent(url).ConfigureAwait(false);
            if (htmlDocument is null) return null;

            var tracksListDiv = htmlDocument.DocumentNode.SelectSingleNode("//div [@class='" + TracksListDivClass + "']");
            var trackListsInternalDiv = tracksListDiv.ChildNodes[1];
            var songBoxes = trackListsInternalDiv.ChildNodes.Where(e => e.GetAttributes().Any(f => f.Value == TrackHtmlClassName));

            return songBoxes;
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
