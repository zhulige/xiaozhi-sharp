using NAudio.Wave;
using System.Threading.Tasks;
using XiaoZhiSharp.Services;

namespace XiaoZhiSharp_MauiBlazorApp.Services
{
    public class AudioService : IDisposable, IAudioService
    {
        // 音频参数
        private const int SampleRate = 24000;
        private const int SampleRate_WaveIn = 16000;
        private const int Bitrate = 16;
        private const int Channels = 1;
        private const int FrameDuration = 60;
        private const int FrameSize = SampleRate_WaveIn * FrameDuration / 1000; // 帧大小

        private WaveInEvent? _waveIn;
        private WaveOutEvent? _waveOut;
        private BufferedWaveProvider? _bufferedWaveProvider;
        private bool _isPlaying;
        private bool _isRecording;

        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;

        public bool IsPlaying => _isPlaying;
        public bool IsRecording => _isRecording;
        public int VadCounter { get; private set; } = 0; // 用于语音活动检测的计数器
        public AudioService()
        {
            try
            {
                // 初始化音频录制组件
                _waveIn = new WaveInEvent()
                {
                    WaveFormat = new WaveFormat(SampleRate_WaveIn, Bitrate, Channels),
                    BufferMilliseconds = FrameDuration
                };
                _waveIn.DataAvailable += OnDataAvailable;

                // 初始化音频播放组件
                _waveOut = new WaveOutEvent();
                _bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(SampleRate, Bitrate, Channels))
                {
                    BufferDuration = TimeSpan.FromSeconds(5) // 5秒缓冲
                };
                _waveOut.Init(_bufferedWaveProvider);

                Console.WriteLine("Windows音频服务初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化Windows音频组件时出错: {ex.Message}");
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (_isRecording && e.BytesRecorded > 0)
                {
                    // 检查是否为静音
                    if (!IsAudioMute(e.Buffer, e.BytesRecorded))
                    {
                        // 创建实际数据大小的字节数组
                        byte[] audioData = new byte[e.BytesRecorded];
                        Array.Copy(e.Buffer, audioData, e.BytesRecorded);
                        
                        // 触发PCM音频事件
                        OnPcmAudioEvent?.Invoke(audioData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理音频数据时出错: {ex.Message}");
            }
        }

        public void StartRecording()
        {
            try
            {
                if (_waveIn != null && !_isRecording)
                {
                    Console.WriteLine("开始录音...");
                    _waveIn.StartRecording();
                    _isRecording = true;
                    Console.WriteLine("Windows录音已开始");
                }
                else
                {
                    Console.WriteLine("无法开始录音，WaveIn未初始化或正在录音中");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动录音时出错: {ex.Message}");
            }
        }

        public void StopRecording()
        {
            try
            {
                if (_waveIn != null && _isRecording)
                {
                    _waveIn.StopRecording();
                    _isRecording = false;
                    Console.WriteLine("Windows录音已停止");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止录音时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 静音检测
        /// </summary>
        private bool IsAudioMute(byte[] buffer, int bytesRecorded)
        {
            double rms = 0;
            int sampleCount = bytesRecorded / 2; // 每个样本 2 字节

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(buffer, i * 2);
                rms += sample * sample;
            }

            rms = Math.Sqrt(rms / sampleCount);
            rms /= short.MaxValue; // 归一化到 0 - 1 范围

            double MuteThreshold = 0.01; // 静音阈值
            return rms < MuteThreshold;
        }

        public void StartPlaying()
        {
            try
            {
                if (!_isPlaying && _waveOut != null)
                {
                    _waveOut.Play();
                    _isPlaying = true;
                    Console.WriteLine("开始播放音频");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动播放时出错: {ex.Message}");
            }
        }

        public void StopPlaying()
        {
            try
            {
                if (_isPlaying && _waveOut != null)
                {
                    _waveOut.Stop();
                    _isPlaying = false;
                    Console.WriteLine("停止播放音频");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止播放时出错: {ex.Message}");
            }
        }

        public void AddOutSamples(byte[] pcmData)
        {
            try
            {
                if (_bufferedWaveProvider != null && pcmData.Length > 0)
                {
                    // 自动开始播放
                    if (!_isPlaying)
                    {
                        StartPlaying();
                    }

                    // 添加音频数据到缓冲区
                    _bufferedWaveProvider.AddSamples(pcmData, 0, pcmData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加音频样本时出错: {ex.Message}");
            }
        }

        public void AddOutSamples(float[] pcmData)
        {
            try
            {
                // 将float数组转换为byte数组
                byte[] byteData = new byte[pcmData.Length * 2]; // 16位音频，每个float转为2字节
                for (int i = 0; i < pcmData.Length; i++)
                {
                    // 将float转换为16位PCM
                    short sample = (short)(pcmData[i] * short.MaxValue);
                    byte[] sampleBytes = BitConverter.GetBytes(sample);
                    byteData[i * 2] = sampleBytes[0];
                    byteData[i * 2 + 1] = sampleBytes[1];
                }
                
                AddOutSamples(byteData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换float音频数据时出错: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                // 停止录音和播放
                if (_isRecording)
                    StopRecording();
                if (_isPlaying)
                    StopPlaying();

                // 释放资源
                _waveIn?.Dispose();
                _waveOut?.Dispose();
                _bufferedWaveProvider?.ClearBuffer();

                Console.WriteLine("Windows音频服务已释放");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放音频资源时出错: {ex.Message}");
            }
        }
    }
} 