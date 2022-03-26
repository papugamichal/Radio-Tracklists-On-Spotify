using System;

namespace RadioTracklistsOnSpotify.Services.SpotifyClientService.DTOs
{
    public class AccessTokenDto
    {
        public void Set(AccessTokenDto token)
        {
            access_token = token.access_token;
            token_type = token.token_type;
            scope = token.scope;
            expires_in = token.expires_in;
            expires_in_datetime = DateTime.UtcNow.AddSeconds(expires_in);
            refresh_token = token.refresh_token;
        }

        public void Update(AccessTokenDto token)
        {
            access_token = token.access_token;
            token_type = token.token_type;
            scope = token.scope;
            expires_in = token.expires_in == 0 ? expires_in : token.expires_in;
            expires_in_datetime = DateTime.UtcNow.AddSeconds(expires_in);
            refresh_token = string.IsNullOrEmpty(token.refresh_token) ? refresh_token : token.refresh_token;
        }

        public void SetCode(string arg)
        {
            autorizationCode = arg;
        }

        public bool IsExpired()
        {
            return expires_in_datetime <= DateTime.UtcNow;
        }

        public DateTime ExpireAtLocalTime()
        {
            return expires_in_datetime.ToLocalTime();
        }

        public string access_token { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
        public long expires_in { get; set; }
        public DateTime expires_in_datetime { get; set; }
        public string refresh_token { get; set; }
        public string autorizationCode { get; set; }
    }
}
