using System.Net;
using System.Net.Http.Json;

namespace Wordrise.Modules.Identity.Tests;

/// <summary>
/// In production a reverse proxy sits in front of the API. The rate limiter counts per client address,
/// so it must see the real visitor (from the proxy's header), not the proxy for everybody.
/// </summary>
public sealed class ProxyHeadersTests
{
    private static WordriseApiFactory Factory(bool trustProxy)
    {
        var factory = new WordriseApiFactory();
        factory.Settings["Identity:AuthRequestsPerMinute"] = "2";
        factory.Settings["Hosting:TrustProxyHeaders"] = trustProxy ? "true" : "false";
        return factory;
    }

    private static async Task<HttpStatusCode> LoginAsync(HttpClient client, string visitor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = "nobody@example.com", password = "wrong-password1" }),
        };
        request.Headers.Add("X-Forwarded-For", visitor);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task Behind_a_trusted_proxy_each_visitor_has_their_own_limit()
    {
        using var factory = Factory(trustProxy: true);
        var client = factory.CreateHttpsClient();

        List<HttpStatusCode> results = [];
        foreach (var visitor in new[] { "203.0.113.1", "203.0.113.2", "203.0.113.3", "203.0.113.4" })
        {
            results.Add(await LoginAsync(client, visitor));
        }

        Assert.DoesNotContain(HttpStatusCode.TooManyRequests, results);
    }

    [Fact]
    public async Task One_visitor_is_still_limited_behind_the_proxy()
    {
        using var factory = Factory(trustProxy: true);
        var client = factory.CreateHttpsClient();

        List<HttpStatusCode> results = [];
        for (var i = 0; i < 4; i++)
        {
            results.Add(await LoginAsync(client, "203.0.113.9"));
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, results);
    }

    [Fact]
    public async Task Without_a_proxy_a_forged_header_cannot_escape_the_limit()
    {
        using var factory = Factory(trustProxy: false);
        var client = factory.CreateHttpsClient();

        List<HttpStatusCode> results = [];
        foreach (var visitor in new[] { "203.0.113.1", "203.0.113.2", "203.0.113.3", "203.0.113.4" })
        {
            results.Add(await LoginAsync(client, visitor));
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, results);
    }
}
