using System.Text;
using System.Text.Json;

namespace CronJob.Template.Infrastructure;

public interface IExampleApiClient
{
    Task<R?> Get<R>(string url);

    Task<R?> Post<T, R>(string url, T body);
    Task Post<T>(string url, T body);
    
    Task<R?> Put<T, R>(string url, T body);
    Task Put<T>(string url, T body);

    Task Delete<R>(string url);
}

public record ExampleApiClient(HttpClient Client) : IExampleApiClient
{
    public async Task<R?> Get<R>(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(BuildUrl(url));
        await CheckResponse(response);
        return await ParseResponse<R>(response);
    }

    public async Task<R?> Post<T, R>(string url, T body)
    {
        string bodyText = Serialize(body);
        HttpContent content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Client.PostAsync(BuildUrl(url), content);
        await CheckResponse(response);
        return await ParseResponse<R>(response);
    }

    public async Task Post<T>(string url, T body)
    {
        string bodyText = Serialize<T>(body);
        HttpContent content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Client.PostAsync(BuildUrl(url), content);
        await CheckResponse(response);
    }

    public async Task<R?> Put<T, R>(string url, T body)
    {
        string bodyText = Serialize(body);
        HttpContent content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Client.PutAsync(BuildUrl(url), content);
        await CheckResponse(response);
        return await ParseResponse<R>(response);
    }

    public async Task Put<T>(string url, T body)
    {
        string bodyText = Serialize<T>(body);
        HttpContent content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Client.PutAsync(BuildUrl(url), content);
        await CheckResponse(response);
    }

    public async Task Delete<R>(string url)
    {
        HttpResponseMessage response = await Client.DeleteAsync(BuildUrl(url));
        await CheckResponse(response);
    }

    private static async Task CheckResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string contentString = await response.Content.ReadAsStringAsync();
        throw new Exception($"Error making request with {nameof(ExampleApiClient)}. Status Code: {response.StatusCode} \n Body: \n{contentString}");
    }

    private static async Task<T?> ParseResponse<T>(HttpResponseMessage response)
    {
        string contentString = await response.Content.ReadAsStringAsync();
        return Deserialize<T>(contentString);
    }

    private static readonly JsonSerializerOptions JSON_SERIALIZER_POLICY = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static T? Deserialize<T>(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(value, JSON_SERIALIZER_POLICY);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unable to parse request in {nameof(ExampleApiClient)}, \nUnparsable value: {value} ", ex);
        }
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JSON_SERIALIZER_POLICY);

    private string BuildUrl(string url) =>
        $"{Client.BaseAddress?.ToString().TrimEnd('/')}/{url.TrimStart('/')}";
}

