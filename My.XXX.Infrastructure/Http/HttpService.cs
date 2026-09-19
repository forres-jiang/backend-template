using Microsoft.Extensions.Logging;
using My.XXX.Shared;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace My.XXX.Infrastructure;

public class HttpService : IHttpService, IScopeDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpService> _logger;

    public HttpService(IHttpClientFactory httpClientFactory, ILogger<HttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<APIResult<T>> Put<T>(RequestModel model) => Send<T>(HttpMethod.Put, model);
    public Task<APIResult<T>> Post<T>(RequestModel model) => Send<T>(HttpMethod.Post, model);

    private async Task<APIResult<T>> Send<T>(HttpMethod method, RequestModel model)
    {
        using var client = _httpClientFactory.CreateClient("External");
        using var request = new HttpRequestMessage(method, model.Url);
        if (!string.IsNullOrEmpty(model.Token))
            request.Headers.TryAddWithoutValidation("Authorization", model.Token);
        if (model.Parameters != null)
            request.Content = JsonContent.Create(model.Parameters);

        var watch = Stopwatch.StartNew();
        using var response = await client.SendAsync(request);
        // URLs, headers and payloads can all carry credentials.
        _logger.LogInformation("External HTTP {Method} returned {StatusCode} in {ElapsedMs} ms",
            method.Method, (int)response.StatusCode, watch.ElapsedMilliseconds);

        if (!response.IsSuccessStatusCode)
            return new APIResult<T> { HttpStatusCode = response.StatusCode, Status = 0, Message = "External request failed." };

        var content = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(content))
            return new APIResult<T> { HttpStatusCode = response.StatusCode, Status = 1 };

        var result = JsonConvert.DeserializeObject<APIResult<T>>(content);
        if (result == null)
            return new APIResult<T> { HttpStatusCode = response.StatusCode, Status = 0, Message = "Empty external response." };
        result.HttpStatusCode = response.StatusCode;
        return result;
    }
}