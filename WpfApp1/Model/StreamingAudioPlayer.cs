using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace WpfApp1.Model
{
    public class StreamingAudioPlayer
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private BufferedWaveProvider _waveProvider;
        private WaveOutEvent _waveOut;
        private bool _isDownloading;

        public async Task DownloadAndPlayAsync(string url, string savePath = null)
        {
            _isDownloading = true;

            // 初始化网络流
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();

            // 初始化文件保存（可选）
            FileStream fileStream = null;
            if (!string.IsNullOrEmpty(savePath))
            {
                fileStream = new FileStream(savePath, FileMode.Create);
            }

            //// 自动检测音频格式
            //using (var reader = new MediaFoundationReader(stream))
            //{
            //    // 初始化播放器
            //    _waveOut = new WaveOutEvent();
            //    _waveOut.Init(reader);
            //    _waveOut.Play();

            //    // 分块读取并保存
            //    byte[] buffer = new byte[4096];
            //    int bytesRead;
            //    while (_isDownloading && (bytesRead = await reader.ToSampleProvider().ReadAsync(buffer, 0, buffer.Length)) > 0)
            //    {
            //        // 写入本地文件（可选）
            //        fileStream?.Write(buffer, 0, bytesRead);
            //    }

            //    fileStream?.Close();
            //}
        }

        public void Stop()
        {
            _isDownloading = false;
            _waveOut?.Stop();
            _waveOut?.Dispose();
        }
    }
}
