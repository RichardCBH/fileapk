using System.Net;
using System.Net.NetworkInformation;
using System.Linq;

namespace HfsAndroid;

public partial class MainPage : ContentPage
{
    private HfsServerService _serverService = new HfsServerService();
    private string _selectedPath = string.Empty;

    public MainPage()
    {
        InitializeComponent();
        LoadIpAddress();
    }

    private void OnStartClicked(object sender, EventArgs e)
    {
        _selectedPath = PathEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(_selectedPath))
        {
            DisplayAlertAsync("提示", "请输入文件夹路径", "OK");
            return;
        }

        _serverService.Start(_selectedPath);
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        StatusLabel.Text = "服务器运行中 (端口 65500)";
    }

    private void OnStopClicked(object sender, EventArgs e)
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
                .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                .Select(addr => addr.Address.ToString())
                .ToList();

            IpLabel.Text = addresses.Count > 0 ? string.Join("\n", addresses) : "未找到 IPv6 地址";
        }
        catch
        {
            IpLabel.Text = "无法获取 IP";
        }
    }
}