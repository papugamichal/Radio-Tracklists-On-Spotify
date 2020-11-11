using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrabHTMLContnetFromWeb
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build()
                ;

            var appsettings = configuration.Get<AppSettings>();

            Console.WriteLine("Hello!");
            Console.WriteLine("Start date: " + appsettings.StartDate + "Endpoint: " + appsettings.PlaylistEndpoint);

            var dateFormat = appsettings.DateFormat;
            var startDate = DateTime.Parse(appsettings.StartDate);
            var todayDate = DateTime.Today;
            var dateRange = Enumerable.Range(0, 1 + todayDate.Subtract(startDate).Days).Select(offset => startDate.AddDays(offset)).ToArray();

            var playlistHistory = new Dictionary<DateTime, IEnumerable<SimpleHistory>>();
            //foreach (var date in dateRange)
            //{

            //    var url = appsettings.PlaylistEndpoint + date.ToString(dateFormat);
            //    var web = new HtmlWeb();
            //    var doc = web.Load(url);
            //    if (web.StatusCode != System.Net.HttpStatusCode.OK)
            //    {
            //        Console.WriteLine($"Something went wrong during download history: {web.StatusCode} from date: '{date.ToString(dateFormat)}'");
            //    }
            //    else
            //    {
            //        var playListNode = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[3]/div[1]/div[2]/div/div/div/div[1]/div/div/div");
            //        var songBoxes = playListNode.ChildNodes.Where(e => e.GetAttributes().Any(f => f.Value == "lista-ogolna-box"));

            //        var playlist = new List<SimpleHistory>();
            //        foreach (var songDetails in songBoxes)
            //        {
            //            var item = new SimpleHistory()
            //            {
            //                PlayTime = date.Add(TimeSpan.Parse(songDetails.ChildNodes[0].InnerHtml)),
            //                ArtistName = songDetails.ChildNodes[1].ChildNodes[0].InnerHtml,
            //                Title = songDetails.ChildNodes[1].ChildNodes[1].InnerHtml
            //            };
            //            playlist.Add(item);
            //        }

            //        playlistHistory.TryAdd(date, playlist);
            //    }
            //}

            //var entityToPlaylist = playlistHistory.First().Value;

            string spotifyToken = GetSpotifyToken();
            //string userID = GetSpotifyUserId(spotifyToken);
            
            var createdSuccesfully = CreateSpotifyPlaylist(spotifyToken, "digestonline94");

            Console.WriteLine($"Bye!");
            Console.ReadKey();
        }


        static string GetSpotifyToken()
        {
            string spotifyApi = "https://accounts.spotify.com/api";
            string clientId = "client_id";
            string clientSecret = "client_secret";
            string credentials = string.Format("{0}:{1}", clientId, clientSecret);

            var client = new RestClient(spotifyApi);
            client.Authenticator = new HttpBasicAuthenticator(clientId, clientSecret);

            var postRequest = new RestRequest("/v1/users/{user_id}/playlists", Method.POST);
            postRequest.AddHeader("content-type", "application/x-www-form-urlencoded");

            postRequest.AddParameter("grant_type", "client_credentials");
            postRequest.AddParameter("scope", "playlist-modify-private");
            var request = client.Execute<AccessToken>(postRequest);

            return request.Data.access_token;
        }

        static bool CreateSpotifyPlaylist(string token, string userId)
        {
            string spotifyApi = "https://api.spotify.com/v1";

                var client = new RestClient(spotifyApi);
                client.Authenticator = new JwtAuthenticator(token);
            var postRequest = new RestRequest($"/users/{userId}/playlists", Method.POST);
            postRequest.Parameters.Clear();
            postRequest.AddHeader("content-type", "application/json");
            postRequest.AddJsonBody(
                new
                {
                    name = "A New Playlist",
                    @public = "false",
                    description = "Description for test playlist"
                });


            var request = client.Execute(postRequest);

            if (request.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }

            return false;
        }

        static string GetSpotifyUserId(string token)
        {
            string spotifyApi = "https://api.spotify.com/v1";

            var client = new RestClient(spotifyApi);

            var getRequest = new RestRequest($"/me", Method.GET);
            getRequest.AddHeader("authorization", "Bearer " + token);
            
            var request = client.Execute(getRequest);

            if (request.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        class AccessToken
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public long expires_in { get; set; }
        }
    }
}
