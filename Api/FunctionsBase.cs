using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
    public abstract class FunctionsBase
    {
        protected readonly IConfiguration configuration;

        public FunctionsBase(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        protected async Task<TokenResult> ValidateToken(        
        HttpRequest req, ILogger log,
        CancellationToken ct = default)
        {
            log.LogInformation("Starting Validating Token");
            
            var issuer = "https://dev-15099932.okta.com/oauth2/default";

            foreach(var header in req.Headers)
                log.LogInformation($"Request Header - [{header.Key}] :: [{header.Value}]");

            var token = req.Headers.ContainsKey("authorization") ?
                        req.Headers["authorization"].First()[7..]
                        : string.Empty;

            log.LogInformation($"Validating [Token]: {token}");

            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                issuer + "/.well-known/oauth-authorization-server",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());

            var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);
            var signingKeys = discoveryDocument.SigningKeys;

            var validationParameters = new TokenValidationParameters
            {
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidateLifetime = true,
                ValidateAudience = false,
                // Allow for some drift in server time
                // (a lower value is better; we recommend two minutes or less)
                ClockSkew = TimeSpan.FromMinutes(2),
                // See additional validation for aud below
            };

            try
            {
                var principal = new JwtSecurityTokenHandler()
                    .ValidateToken(token, validationParameters, out var rawValidatedToken);

                log.LogInformation($"Validating Token Result: {JsonConvert.SerializeObject(rawValidatedToken)}");

                return new TokenResult
                {
                    Active = rawValidatedToken != null
                };
            }
            catch (SecurityTokenValidationException ex)
            {
                log.LogInformation($"SecurityTokenValidationException: {ex.Message}");
            }
            catch (Exception ex) 
            {
                log.LogInformation($"Validating Token Exception: {ex.Message}");
            }

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
            public static TokenResult Success()
            {
                return new TokenResult
                {
                    Active = true
                };
            }
        }
    }
}
