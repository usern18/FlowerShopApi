
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FlowerShopApi.Tests;

public class ProductTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] =
                        "TestOnlyJwtSigningKey_AtLeast32Characters_Long",
                    ["Jwt:Issuer"] = "FlowerShopApi",
                    ["Jwt:Audience"] = "FlowerShopApiUsers"
                });
        });
    }
}
