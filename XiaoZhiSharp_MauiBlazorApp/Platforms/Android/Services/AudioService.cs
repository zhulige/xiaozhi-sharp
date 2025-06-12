using Android.App;
using Android.Content.PM;
using Android.Media;
using AndroidX.Core.App;
using AndroidX.Core.Content;
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
        private const int FrameSize = SampleRate * FrameDuration / 1000; // 帧大小

        // 计算60ms的样本数
        private const int SamplesPerFrame = SampleRate_WaveIn * FrameDuration / 1000;
        // 16位音频每个样本2字节
        private const int BytesPerSample = Bitrate / 8;
        // 60ms帧的字节数
        private const int BytesPerFrame = SamplesPerFrame * BytesPerSample;

        private MediaPlayer? _mediaPlayer;
        private AudioRecord? _audioRecord;
        private AudioTrack? _audioTrack;
        private bool _isPlaying;
        private bool _isRecording;

        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;

        public bool IsPlaying => _isPlaying;
        public bool IsRecording => _isRecording;
        public int VadCounter { get; private set; } = 0; // 用于语音活动检测的计数器
        public AudioService()
        {
            if (!HasAudioPermission())
            {
                RequestAudioPermission();
            }

            try
            {
                // 初始化音频播放组件
                _mediaPlayer = new MediaPlayer();
                // 计算最小缓冲区大小，并确保至少能容纳一帧数据
                int minBufferSize = AudioRecord.GetMinBufferSize(SampleRate_WaveIn, ChannelIn.Mono, Android.Media.Encoding.Pcm16bit);
                int bufferSize = Math.Max(minBufferSize, BytesPerFrame * 2); // 至少能容纳2帧数据

                // 初始化音频录制组件
                _audioRecord = new AudioRecord(AudioSource.Mic, SampleRate_WaveIn, ChannelIn.Mono, Android.Media.Encoding.Pcm16bit, bufferSize);

                // 检查 AudioRecord 是否初始化成功
                if (_audioRecord.State != State.Initialized)
                {
                    Console.WriteLine("AudioRecord 初始化失败");
                    _audioRecord = null;
                }

                // 初始化 AudioTrack
                int audioTrackBufferSize = AudioTrack.GetMinBufferSize(SampleRate, ChannelOut.Mono, Android.Media.Encoding.Pcm16bit);
                _audioTrack = new AudioTrack(Android.Media.Stream.Music, SampleRate, ChannelOut.Mono, Android.Media.Encoding.Pcm16bit, audioTrackBufferSize, AudioTrackMode.Stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化音频组件时出错: {ex.Message}");
            }
        }

        private bool HasAudioPermission()
        {
            return ContextCompat.CheckSelfPermission(Android.App.Application.Context, Android.Manifest.Permission.RecordAudio) == Permission.Granted;
        }
        private async Task RequestAudioPermission()
        {
            ActivityCompat.RequestPermissions(Platform.CurrentActivity, new string[] { Android.Manifest.Permission.RecordAudio }, 1);
            await Task.Delay(1000); // 等待用户响应权限请求
        }
        public void StartRecording()
        {
            try
            {
                // 确保 AudioRecord 已经正确初始化且未在录音中
                if (_audioRecord != null && _audioRecord.State == State.Initialized && !_isRecording)
                {
                    Console.WriteLine("尝试开始录音");
                    _audioRecord.StartRecording();
                    _isRecording = true;
                    Console.WriteLine("开始录音");
                    VadCounter = 0; // 重置静音计数器

                    Task.Run(() =>
                    {
                        byte[] buffer = new byte[FrameSize];
                        while (_isRecording)
                        {
                            try
                            {
                                int bytesRead = _audioRecord.Read(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    if (!IsAudioMute(buffer, bytesRead))
                                    {
                                        if (OnPcmAudioEvent != null)
                                        {
                                            OnPcmAudioEvent(buffer);
                                        }
                                    }
                                    else
                                    {
                                        VadCounter++; // 增加静音计数器
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"录音时出错: {ex.Message}");
                            }
                        }
                    });
                }
                else
                {
                    if (_audioRecord == null)
                    {
                        Console.WriteLine("AudioRecord 未初始化，可能是权限问题或资源冲突");
                    }
                    else if (_audioRecord.State != State.Initialized)
                    {
                        Console.WriteLine($"AudioRecord 状态异常: {_audioRecord.State}");
                    }
                    else if (_isRecording)
                    {
                        Console.WriteLine("正在录音中，无法再次开始");
                    }
                    Console.WriteLine("无法开始录音，AudioRecord 未初始化或正在录音中");
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
                if (_audioRecord != null && _isRecording)
                {
                    _audioRecord.Stop();
                    _isRecording = false;
                    Console.WriteLine("结束录音");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停止录音时出错: {ex.Message}");
            }
        }
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
                if (!_isPlaying && _audioTrack != null)
                {
                    _audioTrack.Play();
                    _isPlaying = true;
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
                if (_isPlaying && _audioTrack != null)
                {
                    _audioTrack.Stop();
                    _isPlaying = false;
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
                if (_audioTrack != null)
                {
                    if (!_isPlaying)
                    {
                        StartPlaying();
                    }
                    _audioTrack.Write(pcmData, 0, pcmData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加样本数据时出错: {ex.Message}");
            }
        }
        public void AddOutSamples(float[] pcmData)
        {
            throw new NotImplementedException("不支持 float 类型的 PCM 数据");
        }
        public void Dispose()
        {
            try
            {
                _isPlaying = false;
                _isRecording = false;
                _audioRecord?.Release();
                _audioRecord = null;
                _mediaPlayer?.Release();
                _mediaPlayer = null;
                _audioTrack?.Release();
                _audioTrack = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放资源时出错: {ex.Message}");
            }
        }
    }
}