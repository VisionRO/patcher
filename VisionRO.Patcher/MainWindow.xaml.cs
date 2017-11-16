using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using VisionRO.Patcher.Services;

namespace VisionRO.Patcher
{
    public partial class MainWindow : Window
    {
        private UpdateService _updateService { get; set; }
        private string _clientUri { get; set; }

        public MainWindow()
        {
            _clientUri = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            _updateService = new UpdateService(_clientUri, "https://github.com/VisionRO/client.git", UpdateProgressDelegate, ClientReadyDelegate);
            InitAsync();
        }

        private async void InitAsync()
        {
            if (!_updateService.IsInstalled())
            {
                if (!_updateService.IsValidRagnarokClient())
                {
                    MessageBox.Show("This does not appear to be a valid Ragnarok Online folder. Please put the patcher in a Korean Ragnarok Online folder.");
                    Close();
                }
                await _updateService.InstallAsync();
            }
            else
            {
                await _updateService.UpdateAsync();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Path.Combine(_clientUri, "vision-ro.exe"));
            Close();
        }

        private async void btnRepair_Click(object sender, RoutedEventArgs e)
        {
            await _updateService.RepairAsync();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UpdateProgressDelegate(string msg, int progress)
        {
            Dispatcher.InvokeAsync(new Action(() =>
            {
                progressLabel.Content = msg;
                progressBar.Value = progress;
            }));
        }

        private void ClientReadyDelegate()
        {
            Dispatcher.InvokeAsync(new Action(() =>
            {
                btnPlay.Visibility = Visibility.Visible;
            }));
        }
    }
}
