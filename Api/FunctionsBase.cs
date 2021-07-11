using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorApp.Api
{
    public class FunctionsBase
    {
        protected static async Task<TokenResult> ValidateToken(        
        HttpRequest req, ILogger log,
        CancellationToken ct = default)
        {
            log.LogInformation("Validating Token");
            
            //Call the Okta introspection API to validate the token.
            var baseUrl = "https://dev-15099932.okta.com/oauth2/default/v1/introspect";
            
            var token = req.Headers.ContainsKey("authorization") ? 
                        req.Headers["authorization"].First()                       
                        : string.Empty;

            log.LogInformation($"Validating Token Full: {token}");

            token = token[7..];

            log.LogInformation($"Validating Token Trimmed: {token}");

            if (string.IsNullOrWhiteSpace(token))
                return TokenResult.Fail();

            var request = new FormUrlEncodedContent(new[]
            {
               new KeyValuePair<string, string>("token", token),
               new KeyValuePair<string, string>("token_type_hint", "access_token"),
               new KeyValuePair<string, string>("client_id", "0oa170e5plBIZCHav5d7")
            });

            var _httpClient = new HttpClient();
            var response = await _httpClient.PostAsync(baseUrl, request);

            log.LogInformation($"Validating Token StatusCode: {response.StatusCode}");            

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TokenResult>(content);
            
            log.LogInformation($"Validating Token Result: {content}");

            if (response.IsSuccessStatusCode)            
                return result;            

            return TokenResult.Fail();            
        }

        public class TokenResult
        {
            public bool Active { get; set; }
            public string  UserName { get; set; }
            [JsonProperty("uid")]
            public string UserId { get; set; }

            public static TokenResult Fail() {
                return new TokenResult
                {
                    Active = false
                };
            }
        }
    }
}
