using Android.App;
using Android.Content;
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
        #region 常量定义
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
        
        // VAD相关字段（从Unity版本移植）
        private bool useVAD = true;                // 是否使用VAD
        private float vadThreshold = 0.015f;       // VAD阈值，可根据环境噪音调整
        private int vadSilenceFrames = 20;         // 静音帧数阈值(约0.33秒)
        private int currentSilenceFrames = 0;      // 当前连续静音帧数
        private bool isSpeaking = false;           // 是否检测到说话
        private DateTime lastTtsEndTime = DateTime.Now; // 上次TTS结束的时间
        private float ttsCooldownTime = 0.5f;      // TTS结束后的冷却时间(秒)
        private bool isInCooldown = false;         // 是否处于冷却期
        private CancellationTokenSource? _recordingCts;
        #endregion
        #region 事件
        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;
        #endregion
        #region 属性
        public bool IsPlaying => _isPlaying;
        public bool IsRecording => _isRecording;
        public int VadCounter { get; private set; } = 0; // 用于语音活动检测的计数器
        #endregion
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
                // 如果处于冷却期，不启动录音
                if (isInCooldown)
                {
                    Console.WriteLine("处于冷却期，暂不启动录音");
                    return;
                }
                
                // 确保 AudioRecord 已经正确初始化且未在录音中
                if (_audioRecord != null && _audioRecord.State == State.Initialized && !_isRecording)
                {
                    Console.WriteLine("尝试开始录音");
                    _audioRecord.StartRecording();
                    _isRecording = true;
                    Console.WriteLine("开始录音");
                    VadCounter = 0; // 重置静音计数器
                    currentSilenceFrames = 0;
                    isSpeaking = false;
                    
                    _recordingCts = new CancellationTokenSource();
                    var token = _recordingCts.Token;

                    Task.Run(() =>
                    {
                        byte[] buffer = new byte[BytesPerFrame];
                        while (_isRecording && !token.IsCancellationRequested)
                        {
                            try
                            {
                                int bytesRead = _audioRecord.Read(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    // 发送音频数据
                                    if (OnPcmAudioEvent != null)
                                    {
                                        OnPcmAudioEvent(buffer);
                                    }

                                    // VAD检测
                                    if (useVAD)
                                    {
                                        bool hasVoice = DetectVoiceActivity(buffer, bytesRead);
                                        
                                        if (hasVoice)
                                    {
                                            currentSilenceFrames = 0;
                                            if (!isSpeaking)
                                            {
                                                // 检查是否在TTS结束后太快检测到声音（可能是回音）
                                                var timeSinceTts = (DateTime.Now - lastTtsEndTime).TotalSeconds;
                                                if (timeSinceTts < ttsCooldownTime * 2)
                                                {
                                                    Console.WriteLine($"VAD: TTS结束后{timeSinceTts:F2}秒内检测到声音，可能是回音，忽略");
                                                    continue;
                                                }
                                                
                                                isSpeaking = true;
                                                Console.WriteLine("VAD: 检测到语音开始");
                                            }
                                        }
                                        else
                                        {
                                            currentSilenceFrames++;
                                            
                                            // 如果之前在说话，且静音超过阈值，认为说话结束
                                            if (isSpeaking && currentSilenceFrames > vadSilenceFrames)
                                            {
                                                isSpeaking = false;
                                                VadCounter = currentSilenceFrames;
                                                Console.WriteLine($"VAD: 检测到语音结束，静音帧数: {currentSilenceFrames}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 使用原有的简单静音检测
                                        if (!IsAudioMute(buffer, bytesRead))
                                        {
                                            VadCounter = 0;
                                        }
                                        else
                                        {
                                            VadCounter++;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"录音时出错: {ex.Message}");
                            }
                        }
                    }, token);
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
                    VadCounter = 0;
                    currentSilenceFrames = 0;
                    isSpeaking = false;
                    
                    // 取消录音任务
                    _recordingCts?.Cancel();
                    _recordingCts?.Dispose();
                    _recordingCts = null;
                    
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
        
        // 添加VAD检测方法（从Unity版本移植）
        private bool DetectVoiceActivity(byte[] buffer, int bytesRecorded)
        {
            // 计算音频能量
            double energy = 0;
            int sampleCount = bytesRecorded / 2; // 16位音频，每个样本2字节
            
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(buffer, i * 2);
                float normalizedSample = sample / (float)short.MaxValue;
                energy += normalizedSample * normalizedSample;
            }
            
            energy /= sampleCount;
            
            // 在TTS播放结束后的一段时间内，提高VAD阈值，减少误触发
            float currentThreshold = vadThreshold;
            var timeSinceTts = (DateTime.Now - lastTtsEndTime).TotalSeconds;
            
            if (timeSinceTts < ttsCooldownTime * 2)
            {
                // 在冷却期后的一段时间内，使用更高的阈值
                currentThreshold = vadThreshold * 1.5f;
                
                // 如果能量值接近阈值但未超过，记录日志但不触发
                if (energy > vadThreshold && energy <= currentThreshold)
                {
                    Console.WriteLine($"VAD: 冷却期内检测到低能量声音，能量值: {energy:F5}，当前阈值: {currentThreshold:F5}");
                }
            }
            
            // 判断能量是否超过阈值
            bool hasVoice = energy > currentThreshold;
            
            // 调试信息
            if (hasVoice)
            {
                Console.WriteLine($"VAD: 检测到声音，能量值: {energy:F5}，当前阈值: {currentThreshold:F5}");
            }
            
            return hasVoice;
        }
        public void StartPlaying()
        {
            try
            {
                if (!_isPlaying && _audioTrack != null)
                {
                    _audioTrack.Play();
                    _isPlaying = true;
                    
                    // 播放开始时，停止录音以避免回音
                    if (_isRecording)
                    {
                        StopRecording();
                    }
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
                    
                    // 记录TTS结束时间，开始冷却期
                    lastTtsEndTime = DateTime.Now;
                    isInCooldown = true;
                    Console.WriteLine($"TTS播放结束，进入冷却期 {ttsCooldownTime} 秒");
                    
                    // 启动冷却期计时器
                    Task.Run(async () =>
                    {
                        await Task.Delay((int)(ttsCooldownTime * 1000));
                        isInCooldown = false;
                        Console.WriteLine("冷却期结束，可以开始新一轮录音");
                    });
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
        // 添加VAD配置方法
        public void ConfigureVAD(bool enabled, float threshold, int silenceFrames, float cooldownTime)
        {
            useVAD = enabled;
            vadThreshold = threshold;
            vadSilenceFrames = silenceFrames;
            ttsCooldownTime = cooldownTime;
            
            Console.WriteLine($"VAD配置更新: 启用={enabled}, 阈值={threshold}, 静音帧数={silenceFrames}, 冷却时间={cooldownTime}秒");
        }
        
        // 获取当前VAD状态
        public bool IsSpeaking => isSpeaking;
        public bool IsInCooldown => isInCooldown;
        public int CurrentSilenceFrames => currentSilenceFrames;
        
        public void Dispose()
        {
            try
            {
                _isPlaying = false;
                _isRecording = false;
                _recordingCts?.Cancel();
                _recordingCts?.Dispose();
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