using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text;

namespace Web.Middleware;

public sealed class HttpGetCachingHandler : DelegatingHandler
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _absoluteTtl;
    private readonly TimeSpan? _slidingTtl;

    public HttpGetCachingHandler(IMemoryCache cache, TimeSpan absoluteTtl, TimeSpan? slidingTtl = null) 
    {
        _cache = cache;
        _absoluteTtl = absoluteTtl;
        _slidingTtl = slidingTtl;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Only cache idempotent GETs without Authorization (adjust if you want to include auth)
        if (request.Method != HttpMethod.Get)
            return await base.SendAsync(request, ct);

        var hasAuth = request.Headers.Authorization is not null;
        if (hasAuth) // avoid caching per-user/private responses by default
            return await base.SendAsync(request, ct);

        var cacheKey = BuildKey(request);

        if (_cache.TryGetValue(cacheKey, out CachedHttpResponse cached))
        {
            return cached.ToHttpResponseMessage(); // fresh clone per request
        }

        var response = await base.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
            var status = response.StatusCode;

            var entry = new CachedHttpResponse(status, body, mediaType);

            var opts = new MemoryCacheEntryOptions().SetAbsoluteExpiration(_absoluteTtl);
            if (_slidingTtl is not null) opts.SetSlidingExpiration(_slidingTtl.Value);

            _cache.Set(cacheKey, entry, opts);
        }

        return response;
    }

    private static string BuildKey(HttpRequestMessage req)
    {
        // Key on full URL + relevant headers if needed
        var url = req.RequestUri!.ToString();
        // Example: also vary by Accept header
        var accept = string.Join(",", req.Headers.Accept.Select(a => a.MediaType));
        return $"httpcache::{url}::accept={accept}";
    }

    private sealed record CachedHttpResponse(HttpStatusCode Status, string Body, string MediaType)
    {
        public HttpResponseMessage ToHttpResponseMessage()
        {
            var msg = new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body, Encoding.UTF8, MediaType)
            };
            return msg;
        }
    }
}