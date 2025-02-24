using NAudio.Wave;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private BufferedWaveProvider bufferedWaveProvider;
        private bool IsBufferNearlyFull
        {
            get
            {
                return bufferedWaveProvider != null &&
                       bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                       < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
            }
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var url = $"{TextApiServer.Text.Trim()}";
            var payload = new
            {
                text = $"{TextSource.Text.Trim()}",
                chunk_length = 200,
                format = "wav",
                references = new object[] { },
                reference_id = "毕业女",
                use_memory_cache = "on",
                normalize = true,
                streaming = true as bool?,
                max_new_tokens = 1024,
                top_p = 0.7,
                repetition_penalty = 1.2,
                temperature = 0.7
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = null;
                    if (payload.streaming == true)
                    {
                        var postRequest = new HttpRequestMessage(HttpMethod.Post, url);
                        postRequest.Content = content;
                        //流媒体请求头
                        response = await client.SendAsync(postRequest, HttpCompletionOption.ResponseHeadersRead);
                    }
                    else
                    {
                        response = await client.PostAsync(url, content);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("请求成功");
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {

                            if (payload.format.ToString() == "mp3")
                            {
                                using (var reader = new Mp3FileReader(stream))
                                using (var waveOut = new WaveOutEvent())
                                {
                                    waveOut.Init(reader);
                                    waveOut.Play();

                                    // 等待播放完成
                                    while (waveOut.PlaybackState == PlaybackState.Playing)
                                    {
                                        await Task.Delay(500);
                                        //Thread.Sleep(100);
                                    }
                                }
                                Debug.WriteLine("End");
                            }
                            else if (payload.format.ToString() == "wav")
                            {

                                #region WAV

                                //// 假设音频格式为 16bit 16kHz 单声道
                                var waveFormat = new WaveFormat(44100, 16, 1);

                                bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                                {
                                    BufferDuration = TimeSpan.FromSeconds(20) // 设置缓冲区大小
                                };

                                using (var waveOut = new WaveOutEvent())
                                {
                                    waveOut.Init(bufferedWaveProvider);
                                    waveOut.Play();
                                    // 5. 持续读取流数据
                                    byte[] buffer = new byte[4096];
                                    int bytesRead;
                                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        if (IsBufferNearlyFull)
                                        {
                                            Debug.WriteLine("Buffer getting full, taking a break");
                                            //await Task.Delay(500);
                                            Thread.Sleep(500);
                                        }
                                        Debug.WriteLine($"Add bytes,length:{bytesRead}");
                                        bufferedWaveProvider.AddSamples(buffer, 0, bytesRead);
                                    }
                                    // 等待播放完成
                                    while (bufferedWaveProvider.BufferedBytes != 0)
                                    {
                                        Debug.WriteLine("等待播放完成");
                                        await Task.Delay(500);
                                    }
                                    Debug.WriteLine("End");
                                }
                                #endregion
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Error: {response.StatusCode}");
                        throw new Exception(response.Content.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}