﻿using System;
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
        private const string VersionNumber = "1.0.2";
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
            lbVersionNumber.Content = VersionNumber;
            _updateService = new UpdateService(_clientUri, "https://github.com/VisionRO/client.git", UpdateProgressDelegate, ClientReadyDelegate);
            InitAsync();
        }

        private async void InitAsync()
        {
            if (!_updateService.IsInstalled())
                await _updateService.InstallAsync();
            else
                await _updateService.UpdateAsync();
            await _updateService.UpdatePatcherAsync();
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
            await _updateService.UpdateAsync();
            await _updateService.UpdatePatcherAsync();
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
