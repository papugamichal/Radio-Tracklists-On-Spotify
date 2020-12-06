using System;

namespace RadioNowySwiatPlaylistBot.Services.SpotifyClientService.DTOs
{
    public class AccessTokenDto
    {
        public void Set(AccessTokenDto token)
        {
            this.access_token = token.access_token;
            this.token_type = token.token_type;
            this.scope = token.scope;
            this.expires_in = token.expires_in;
            this.expires_in_datetime = DateTime.UtcNow.AddSeconds(expires_in);
            this.refresh_token = token.refresh_token;
        }

        public void Update(AccessTokenDto token)
        {
            this.access_token = token.access_token;
            this.token_type = token.token_type;
            this.scope = token.scope;
            this.expires_in = token.expires_in == 0 ? this.expires_in : token.expires_in;
            this.expires_in_datetime = DateTime.UtcNow.AddSeconds(expires_in);
            this.refresh_token = string.IsNullOrEmpty(token.refresh_token) ? this.refresh_token : token.refresh_token;
        }

        public void SetCode(string arg)
        {
            this.autorizationCode = arg;
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
