using CommunityToolkit.Maui.Storage;
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

    private async void OnSelectFolderClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync();
            if (result != null && !string.IsNullOrEmpty(result.Folder.Path))
            {
                _selectedPath = result.Folder.Path;
                SelectedPathLabel.Text = _selectedPath;
                StartButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", ex.Message, "OK");
        }
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedPath))
        {
            await DisplayAlertAsync("提示", "请先选择文件夹", "OK");
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