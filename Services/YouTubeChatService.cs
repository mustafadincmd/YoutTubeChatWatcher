using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using YouTubeLiveChatWatcher.Hubs;
using YouTubeLiveChatWatcher.Models;

namespace YouTubeLiveChatWatcher.Services;

public class YouTubeChatService
{
    private readonly HttpClient _httpClient;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly string _apiKey = "AIzaSyDkQG5ma71MFn_hFWkuNTeYnOB5wqEHks4";

    public YouTubeChatService(HttpClient httpClient, IHubContext<ChatHub> hubContext)
    {
        _httpClient = httpClient;
        _hubContext = hubContext;
    }

    public async Task<string?> GetLiveChatIdAsync(string videoId)
    {
        var url = $"https://www.googleapis.com/youtube/v3/videos?part=liveStreamingDetails&id={videoId}&key={_apiKey}";
        var response = await _httpClient.GetStringAsync(url);
        var root = JsonDocument.Parse(response).RootElement;

        try
        {
            return root.GetProperty("items")[0]
                .GetProperty("liveStreamingDetails")
                .GetProperty("activeLiveChatId")
                .GetString();
        }
        catch
        {
            return null;
        }
    }

    // Burada artık matchLimit yok, sonsuz dinleme için task başlatıyoruz
    public async Task StartListeningAsync(string liveChatId, string keyword, CancellationToken cancellationToken)
    {
        string? nextPageToken = null;
        int order = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            string url = $"https://www.googleapis.com/youtube/v3/liveChat/messages?liveChatId={liveChatId}&part=snippet,authorDetails&key={_apiKey}";

            if (!string.IsNullOrEmpty(nextPageToken))
                url += $"&pageToken={nextPageToken}";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var root = JsonDocument.Parse(response).RootElement;

                nextPageToken = root.GetProperty("nextPageToken").GetString();

                foreach (var item in root.GetProperty("items").EnumerateArray())
                {
                    string author = item.GetProperty("authorDetails").GetProperty("displayName").GetString();
                    string message = item.GetProperty("snippet").GetProperty("displayMessage").GetString();
                    string timestamp = item.GetProperty("snippet").GetProperty("publishedAt").GetString();

                    if (message.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        order++;

                        var chatMatch = new ChatMatch
                        {
                            Order = order,
                            Author = author,
                            Message = message,
                            Time = timestamp
                        };

                        // SignalR ile frontend'e gönder
                        await _hubContext.Clients.All.SendAsync("ReceiveMessage", chatMatch);
                    }
                }

                await Task.Delay(3000, cancellationToken); // 3 saniyede bir yeni mesajları çek
            }
            catch (TaskCanceledException)
            {
                // İptal edildi, döngü kırılır
                break;
            }
            catch (Exception ex)
            {
                // Hata durumunda log vs yapabilirsin
                Console.WriteLine($"Hata: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }
}
