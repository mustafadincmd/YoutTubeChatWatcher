using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using YouTubeLiveChatWatcher.Services;

namespace YouTubeLiveChatWatcher.Controllers;

public class HomeController : Controller
{
    private readonly YouTubeChatService _chatService;
    private static CancellationTokenSource _cts;

    public HomeController(YouTubeChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    public async Task<IActionResult> StartListening(string videoUrl, string keyword, int matchLimit)
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        string? videoId = ExtractVideoId(videoUrl);
        if (videoId == null)
            return Content("Geçerli bir video URL'si girin.");

        string? liveChatId = await _chatService.GetLiveChatIdAsync(videoId);
        if (liveChatId == null)
            return Content("Canlı yayın bulunamadı veya LiveChatId alınamadı.");

        _cts = new CancellationTokenSource();

        // İşte buraya ekledik:
        _chatService.SetListeningStartTime(DateTime.UtcNow);

        _ = _chatService.StartListeningAsync(liveChatId, keyword ?? string.Empty, matchLimit, _cts.Token);

        return RedirectToAction("Index");
    }



    private string? ExtractVideoId(string url)
    {
        try
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (query["v"] != null)
                return query["v"];
            if (uri.Host.Contains("youtu.be"))
                return uri.AbsolutePath.Trim('/');
        }
        catch { }
        return null;
    }
}
