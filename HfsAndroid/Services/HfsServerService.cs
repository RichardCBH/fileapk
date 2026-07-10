using System.Net;
using System.Text;

namespace HfsAndroid;

public class HfsServerService
{
    private HttpListener? _listener;
    private bool _isRunning = false;
    private string _rootPath = string.Empty;

    public void Start(string rootPath)
    {
        _rootPath = rootPath;
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:65500/");
        _listener.Start();
        _isRunning = true;

        Task.Run(() => ListenAsync());
    }

    public void Stop()
    {
        _isRunning = false;
        _listener?.Stop();
    }

    private async Task ListenAsync()
    {
        while (_isRunning && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                await ProcessRequest(context);
            }
            catch { }
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath.TrimStart('/') ?? "";

        var fullPath = Path.Combine(_rootPath, path);

        if (Directory.Exists(fullPath))
        {
            await ServeDirectoryListing(response, fullPath, path);
        }
        else if (File.Exists(fullPath))
        {
            await ServeFile(response, fullPath);
        }
        else
        {
            response.StatusCode = 404;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Not Found"));
        }

        response.Close();
    }

    private async Task ServeDirectoryListing(HttpListenerResponse response, string dirPath, string relativePath)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body><h1>Index of /").Append(relativePath).Append("</h1><ul>");

        foreach (var dir in Directory.GetDirectories(dirPath))
        {
            var name = Path.GetFileName(dir);
            sb.Append($"<li><a href=\"{name}/\">{name}/</a></li>");
        }
        foreach (var file in Directory.GetFiles(dirPath))
        {
            var name = Path.GetFileName(file);
            sb.Append($"<li><a href=\"{name}\">{name}</a></li>");
        }

        sb.Append("</ul></body></html>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        response.ContentType = "text/html";
        await response.OutputStream.WriteAsync(bytes);
    }

    private async Task ServeFile(HttpListenerResponse response, string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        response.ContentType = GetMimeType(filePath);
        await response.OutputStream.WriteAsync(bytes);
    }

    private string GetMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLower() switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}