using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using YouTubeLiveChatWatcher.Hubs;
using YouTubeLiveChatWatcher.Models;

namespace YouTubeLiveChatWatcher.Services;

public class YouTubeChatService
{
    private readonly HttpClient _httpClient;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly string _apiKey = ""; //  YouTube Data API key here

    private DateTime? _listeningStartTime;

    public YouTubeChatService(HttpClient httpClient, IHubContext<ChatHub> hubContext)
    {
        _httpClient = httpClient;
        _hubContext = hubContext;
    }

    public void SetListeningStartTime(DateTime startTime)
    {
        _listeningStartTime = startTime;
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

    public async Task StartListeningAsync(string liveChatId, string keyword, int matchLimit, CancellationToken cancellationToken)
{
    string? nextPageToken = null;
    int order = 0;
    var seenMessageIds = new HashSet<string>();
    var seenAuthors = new HashSet<string>(StringComparer.OrdinalIgnoreCase); //

    if (_listeningStartTime == null)
    {
        _listeningStartTime = DateTime.UtcNow;
    }

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
                string messageId = item.GetProperty("id").GetString();

                if (seenMessageIds.Contains(messageId))
                    continue;

                seenMessageIds.Add(messageId);

                string author = item.GetProperty("authorDetails").GetProperty("displayName").GetString();
                string message = item.GetProperty("snippet").GetProperty("displayMessage").GetString();
                string timestampStr = item.GetProperty("snippet").GetProperty("publishedAt").GetString();

                if (!DateTime.TryParse(timestampStr, out var timestamp))
                {
                    timestamp = DateTime.UtcNow;
                }

                if (timestamp < _listeningStartTime)
                    continue;

                if (message.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    if (seenAuthors.Contains(author))
                        continue;

                    seenAuthors.Add(author);

                    order++;

                    var chatMatch = new ChatMatch
                    {
                        Order = order,
                        Author = author,
                        Message = message,
                        Time = timestampStr
                    };

                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", chatMatch);

                    if (order >= matchLimit)
                        return;
                }
            }

            await Task.Delay(3000, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            await Task.Delay(5000, cancellationToken);
        }
    }
}

}
