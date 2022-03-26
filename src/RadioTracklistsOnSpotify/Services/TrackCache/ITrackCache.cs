namespace RadioTracklistsOnSpotify.Services.TrackCache
{
    public interface ITrackCache
    {
        void AddToCache(string artist, string beat, string spotifyTrackUri);
        bool InCache(string artist, string beat);
        string GetFromCache(string artis, string beat);
    }
}
