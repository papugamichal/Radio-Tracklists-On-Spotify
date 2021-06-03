using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RadioNowySwiatAutomatedPlaylist.Services.TrackCache
{
    public abstract class TrackCache : ITrackCache
    {
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<Guid, CacheEntity> cache;
        private readonly Timer timer;
        private readonly string serializedCachePath;
        private readonly string serializedFileName;

        public TrackCache(
            ILogger logger, string serializedFileName = "cache"
            )
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serializedFileName = serializedFileName.Contains(".") ? serializedFileName : serializedFileName + ".json";
            serializedCachePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), serializedFileName);
            timer = new Timer(DoWork, null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(5));

            if (File.Exists(serializedCachePath))
            {
                try
                {
                    var rawContent = File.ReadAllText(serializedCachePath);
                    var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<Guid, CacheEntity>>(rawContent);
                    cache = new ConcurrentDictionary<Guid, CacheEntity>(deserialized);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Something went wrong during '{serializedFileName}' deserialization");
                    cache = new ConcurrentDictionary<Guid, CacheEntity>();
                }
            }
            else
            {
                cache = new ConcurrentDictionary<Guid, CacheEntity>();
            }
        }

        public void AddToCache(string artist, string beat, string spotifyTrackUri)
        {
            var hash = GetHash(artist, beat);

            if (cache.ContainsKey(hash))
            {
                return;
            }

            var entity = new CacheEntity
            {
                ArtistName = artist,
                TrackName = beat,
                SpotifyTrackUri = spotifyTrackUri
            };

            cache.TryAdd(hash, entity);

            logger.LogInformation($"Beat added to cache - artis: '{artist}' track: '{beat}'. Cache contains {cache.Count} elements.");
        }

        public string GetFromCache(string artist, string beat)
        {
            var cacheCode = GetHash(artist, beat);
            var exists = cache.TryGetValue(cacheCode, out var entity);

            if (!exists)
            {
                return null;
            }

            return entity.SpotifyTrackUri;
        }

        public bool InCache(string artist, string beat)
        {
            var cacheCode = GetHash(artist, beat);
            return cache.ContainsKey(cacheCode);
        }

        private Guid GetHash(string artist, string beat)
        {
            var toHash = string.Format("{0}{1}", artist, beat).ToLower();

            byte[] stringbytes = Encoding.UTF8.GetBytes(toHash);
            byte[] hashedBytes = new SHA1CryptoServiceProvider()
                .ComputeHash(stringbytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }

        private void DoWork(object state)
        {
            _ = DoWork();
        }

        private async Task DoWork()
        {
            try
            {
                var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(cache, Newtonsoft.Json.Formatting.Indented);
                await File.WriteAllTextAsync(serializedCachePath, serialized);
                logger.LogInformation("Cache content has been serialized.");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Something went wrong during '{serializedFileName}' deserialization");
            }
        }
    }

    internal class CacheEntity
    {
        public string ArtistName { get; set; }
        public string TrackName { get; set; }
        public string SpotifyTrackUri { get; set; }
    }
}
