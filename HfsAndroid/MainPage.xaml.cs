using System.Net.NetworkInformation;

namespace HfsAndroid;

public partial class MainPage : ContentPage
{
    private readonly HfsServerService _serverService = new();
    private string _selectedPath = string.Empty;

    public MainPage()
    {
        InitializeComponent();
        LoadIpAddress();
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        _selectedPath = PathEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(_selectedPath))
        {
            await DisplayAlertAsync("提示", "请输入文件夹路径", "OK");
            return;
        }

        if (!Directory.Exists(_selectedPath))
        {
            await DisplayAlertAsync("提示", "文件夹路径不存在", "OK");
            return;
        }

        try
        {
            _serverService.Start(_selectedPath);
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusLabel.Text = "服务器运行中 (端口 65500)";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("启动失败", ex.Message, "OK");
        }
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _serverService.Stop();
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        StatusLabel.Text = "服务器已停止";
    }

    private void LoadIpAddress()
    {
        try
        {
            var addresses = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                               && !addr.Address.IsIPv6LinkLocal)
                .Select(addr => addr.Address.ToString())
                .Distinct()
                .ToList();

            IpLabel.Text = addresses.Count > 0 ? string.Join("\n", addresses) : "未找到 IPv6 地址";
        }
        catch
        {
            IpLabel.Text = "无法获取 IP";
        }
    }
}
