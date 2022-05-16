using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadioTracklistsOnSpotify.Services.DataSourceService.Abstraction;
using RadioTracklistsOnSpotify.Services.DataSourceService.Configuration;
using RadioTracklistsOnSpotify.Services.DataSourceService.DTOs;
using RestSharp;

namespace RadioTracklistsOnSpotify.Services.DataSourceService
{
    public class RadioNowySwiatDirectDataSourceService : IDataSourceService
    {
        private readonly ILogger<RadioNowySwiatDirectDataSourceService> logger;
        private readonly DataSourceOptions options;

        public RadioNowySwiatDirectDataSourceService(
            ILogger<RadioNowySwiatDirectDataSourceService> logger,
            IOptions<DataSourceOptions> options)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        }

        public async Task<IReadOnlyList<TrackInfo>> GetPlaylistFor(DateTime date)
        {
            var url = GetDataSourceUrlFor();
            var trackHtmlBoxes = await GetDataSourceHtmlElementCollection(url, date);
            var trackCollection = RetriveTracksInfoFrom(trackHtmlBoxes);
            return trackCollection.ToList();
        }

        private string GetDataSourceUrlFor()
        {
            return options.PlaylistEndpoint;
        }

        private async Task<IEnumerable<HtmlNode>> GetDataSourceHtmlElementCollection(string url, DateTime date)
        {
            var htmlDocument = await GetRawContent(url, date).ConfigureAwait(false);
            if (htmlDocument is null) return null;
            var tracksListDiv2 = htmlDocument.DocumentNode.SelectSingleNode("//ul [@class='js-filter rns-vote-list']");

            var tracksAsLiElements = tracksListDiv2.ChildNodes.Where(node => node.Name == "li");
            return tracksAsLiElements;
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
                string title = node.ChildNodes[3].ChildNodes[1].InnerHtml.Trim();
                string artis = node.ChildNodes[3].ChildNodes[3].InnerHtml.Trim();
                string time = node.ChildNodes[5].ChildNodes[1].InnerHtml;

                if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(artis)) continue;
                var playTime = TimeSpan.Parse(time);
                var playDateTime = new DateTime().Add(playTime);

                var item = new TrackInfo(artis, title, playDateTime);
                collection.Add(item);
            }
            return collection;
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

        private async Task<HtmlDocument> GetRawContent(string url, DateTime date)
        {
            var document = new HtmlDocument();
            var sw = new Stopwatch();
            try
            {
                using var client = new RestClient(url);
                var request = new RestRequest();
                request.AddParameter("date", date.ToString("yyyy-MM-dd"), ParameterType.RequestBody);
                request.AddParameter("time_range", date.ToString("Wszystkie"), ParameterType.RequestBody);
                var result = await client.PostAsync(request);

                logger.LogInformation($"Performance monitor. Load HTML document from '{url}' in: {sw.ElapsedMilliseconds} ms");
                if (result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    logger.LogError($"Request to load HTML document from '{url}' end with code: '{result.StatusCode}'");
                    return null;
                }

                document.LoadHtml(result.Content);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Unexpected error occured during fetch HTML document from '{url}'!");
            }

            return document;
        }
    }
}
