using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Text;
using CounterStrikeSharp.API.Modules.Entities;

public class CS2StatsAPIClient {
    private readonly HttpClient httpClient;

    public CS2StatsAPIClient(string authKey, string baseURL) {
        httpClient = new HttpClient {
            BaseAddress = new Uri(baseURL),
            Timeout = TimeSpan.FromMinutes(1)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authKey);
    }

    public async Task UploadMatchJSONAsync(string matchJSON) {
        var jsonBytes = Encoding.UTF8.GetBytes(matchJSON);

        using var compressedStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true)) {
            await gzipStream.WriteAsync(jsonBytes);
        }
        compressedStream.Position = 0;

        using var content = new StreamContent(compressedStream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        content.Headers.ContentEncoding.Add("gzip");

        var response = await httpClient.PostAsync("/upload_match", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task UploadPlayerJSONAsync(ulong playerID) {
        var response = await httpClient.PostAsJsonAsync("/upload_player", new { playerID });
        response.EnsureSuccessStatusCode();
    }

}