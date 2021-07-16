using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cybersource.Models
{
    public class CybersourceToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("refresh_token_expires_in")]
        public long RefreshTokenExpiresIn { get; set; }

        [JsonProperty("client_status")]
        public string ClientStatus { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}
