using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace SIG.BlazorApp.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            var baseAddress = builder.Configuration["BaseAddress"] ?? builder.HostEnvironment.BaseAddress;

            builder.Services.AddScoped<CustomAuthorizationMessageHandler>();
            builder.Services.AddScoped<CorsRequestAuthorizationMessageHandler>();

            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddBlazoredSessionStorage();

            builder.Services.AddHttpClient("WebAPI",
                    client => client.BaseAddress = new Uri(baseAddress))
                .AddHttpMessageHandler<CustomAuthorizationMessageHandler>()
                //- For cross-domain requests to Api Server use this piece of code:
                .AddHttpMessageHandler<CorsRequestAuthorizationMessageHandler>();

                        
            //- For same domain requests use the following code instead:
            //builder.Services
            //    .AddHttpClient("BlazorClient.ServerApi", client => client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ServerApi:BaseAddress")))
            //    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            builder.Services.AddOidcAuthentication(options =>
            {
                // Replace the Okta placeholders with your Okta values in the appsettings.json file.
                options.ProviderOptions.Authority = builder.Configuration.GetValue<string>("Okta:Authority");
                options.ProviderOptions.ClientId = builder.Configuration.GetValue<string>("Okta:ClientId"); ;
                options.ProviderOptions.ResponseType = "code";
            });

            builder.Services.AddApiAuthorization();

            await builder.Build().RunAsync();
        }
    }
}
