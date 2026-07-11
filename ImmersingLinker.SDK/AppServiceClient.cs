namespace ImmersingLinker.SDK;

public class AppServiceClient
{
    private readonly HttpClient _http;

    public AppServiceClient(string port)
    {
        _http = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") };
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            var response = await _http.GetAsync("/app/hello");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}