using System.Net;
using System.Text;

namespace HfsAndroid;

public class HfsServerService
{
    private HttpListener? _listener;
    private bool _isRunning;
    private string _rootPath = string.Empty;
    private CancellationTokenSource? _cts;

    public void Start(string rootPath)
    {
        if (_isRunning)
            return;

        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"路径不存在: {rootPath}");

        _rootPath = rootPath;
        _listener = new HttpListener();
        // Bind to all interfaces on port 65500
        _listener.Prefixes.Add("http://+:65500/");
        _listener.Start();
        _isRunning = true;
        _cts = new CancellationTokenSource();

        _ = Task.Run(() => ListenAsync(_cts.Token));
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch
        {
            // ignore shutdown errors
        }
        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && _listener != null && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = ProcessRequest(context);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // ignore per-request accept errors while running
                if (!_isRunning || cancellationToken.IsCancellationRequested)
                    break;
            }
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            var path = Uri.UnescapeDataString(request.Url?.AbsolutePath.TrimStart('/') ?? "");
            // Prevent path traversal
            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path));
            if (!fullPath.StartsWith(Path.GetFullPath(_rootPath), StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = 403;
                var denied = Encoding.UTF8.GetBytes("Forbidden");
                await response.OutputStream.WriteAsync(denied);
                return;
            }

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
                var notFound = Encoding.UTF8.GetBytes("Not Found");
                await response.OutputStream.WriteAsync(notFound);
            }
        }
        catch (Exception ex)
        {
            try
            {
                response.StatusCode = 500;
                var err = Encoding.UTF8.GetBytes(ex.Message);
                await response.OutputStream.WriteAsync(err);
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            try { response.Close(); } catch { /* ignore */ }
        }
    }

    private static async Task ServeDirectoryListing(HttpListenerResponse response, string dirPath, string relativePath)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Index of /")
          .Append(System.Net.WebUtility.HtmlEncode(relativePath))
          .Append("</title></head><body><h1>Index of /")
          .Append(System.Net.WebUtility.HtmlEncode(relativePath))
          .Append("</h1><ul>");

        if (!string.IsNullOrEmpty(relativePath))
        {
            sb.Append("<li><a href=\"../\">../</a></li>");
        }

        foreach (var dir in Directory.GetDirectories(dirPath).OrderBy(d => d))
        {
            var name = Path.GetFileName(dir);
            var encoded = Uri.EscapeDataString(name);
            sb.Append("<li><a href=\"")
              .Append(encoded)
              .Append("/\">")
              .Append(System.Net.WebUtility.HtmlEncode(name))
              .Append("/</a></li>");
        }

        foreach (var file in Directory.GetFiles(dirPath).OrderBy(f => f))
        {
            var name = Path.GetFileName(file);
            var encoded = Uri.EscapeDataString(name);
            sb.Append("<li><a href=\"")
              .Append(encoded)
              .Append("\">")
              .Append(System.Net.WebUtility.HtmlEncode(name))
              .Append("</a></li>");
        }

        sb.Append("</ul></body></html>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }

    private static async Task ServeFile(HttpListenerResponse response, string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        response.ContentType = GetMimeType(filePath);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }

    private static string GetMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".txt" => "text/plain; charset=utf-8",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".apk" => "application/vnd.android.package-archive",
            _ => "application/octet-stream"
        };
    }
}
