using XiaoZhiSharp.Utils;
using NAudio.Wave;
using OpusSharp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public class AudioWaveService : IAudioService, IDisposable
    {
        // NAudio 音频输出相关组件
        private IWavePlayer? _waveOut;
        private BufferedWaveProvider? _waveOutProvider = null;
        // NAudio 音频输入相关组件
        private WaveInEvent? _waveIn;

        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;
        // 音频参数
        public int SampleRate { get; set; } = 24000;
        public int SampleRate_WaveIn { get; set; } = 16000;
        public int Bitrate { get; set; } = 16;
        public int Channels { get; set; } = 1;
        public int FrameDuration { get; set; } = 60;
        public int FrameSize
        {
            get
            {
                return SampleRate * FrameDuration / 1000; // 帧大小
            }
        }
        public bool IsPlaying { get; private set; }
        public bool IsRecording { get; private set; } = false;
        public AudioWaveService()
        {
            Initialize();
        }
        //public AudioWaveService(int sampleRate, bool isWaveIn, int bitrate = 16, int channels = 1, int frameDuration = 60)
        //{
        //    SampleRate = sampleRate;
        //    Bitrate = bitrate;
        //    Channels = channels;
        //    FrameDuration = frameDuration;

        //    Initialize();
        //}
        public void Initialize()
        {
            // 初始化音频输出相关组件
            var waveFormat = new WaveFormat(SampleRate, Bitrate, Channels);
            _waveOut = new WaveOutEvent();
            _waveOutProvider = new BufferedWaveProvider(waveFormat);
            _waveOut.Init(_waveOutProvider);
            // 增大缓冲区大小，例如设置为 10 秒的音频数据
            _waveOutProvider.BufferLength = SampleRate * Channels * 2 * 10;

            // 初始化音频输入相关组件
            _waveIn = new WaveInEvent();
            _waveIn.WaveFormat = new WaveFormat(48000, Bitrate, Channels);
            //_waveIn.WaveFormat = new WaveFormat(SampleRate, Bitrate, Channels);
            _waveIn.DataAvailable += waveIn_DataAvailable;
            _waveIn.RecordingStopped += waveIn_RecordingStopped;


            // 启动音频播放线程
            Thread threadWave = new Thread(() =>
            {
                while (true)
                {
                    if (!IsPlaying)
                    {
                        if (_waveOutProvider.BufferedDuration > TimeSpan.FromSeconds(1))
                        {
                            StartPlaying();
                        }
                    }
                    while (IsPlaying)
                    {
                        // 可以添加更多逻辑，如缓冲区检查等
                        Thread.Sleep(10);
                    }
                    StopPlaying();
                }
            });
            threadWave.Start();
        }
        public void StartRecording()
        {
            if (_waveIn != null)
            {
                if (!IsRecording)
                {
                    _waveIn.StartRecording();
                    IsRecording = true;
                    //LogConsole.WriteLine("开始录音");
                }
            }
        }
        public void StopRecording()
        {
            if (_waveIn != null)
            {
                if (IsRecording)
                {
                    _waveIn.StopRecording();
                    //LogConsole.WriteLine("结束录音");
                    IsRecording = false;
                }
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
        private void waveIn_RecordingStopped(object? sender, StoppedEventArgs e)
        {
        }
        private float[] ConvertBytesToFloats(byte[] byteData)
        {
            int sampleCount = byteData.Length / 2; // 假设是 16 位音频
            float[] floatData = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(byteData, i * 2);
                floatData[i] = sample / (float)short.MaxValue;
            }

            return floatData;
        }
        private void waveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            Task.Run(() =>
            {
                byte[] pcmBytes48000 = e.Buffer;
                if (!IsAudioMute(pcmBytes48000, e.BytesRecorded))
                {
                    Console.Title = "录音";
                    byte[] pcmBytes = ConvertPcmSampleRate(pcmBytes48000, 48000, SampleRate_WaveIn, Channels, Bitrate);

                    if (OnPcmAudioEvent != null)
                    {
                        OnPcmAudioEvent(pcmBytes);
                    }
                }
                else
                {
                    Console.Title = "静音";
                }
            });
        }
        private byte[] ConvertPcmSampleRate(byte[] pcmData, int originalSampleRate, int targetSampleRate, int channels, int bitsPerSample)
        {
            // 创建原始音频格式
            WaveFormat originalFormat = new WaveFormat(originalSampleRate, bitsPerSample, channels);

            // 将 byte[] 数据包装成 MemoryStream
            using (MemoryStream memoryStream = new MemoryStream(pcmData))
            {
                // 创建原始音频流
                using (RawSourceWaveStream originalStream = new RawSourceWaveStream(memoryStream, originalFormat))
                {
                    // 创建目标音频格式
                    WaveFormat targetFormat = new WaveFormat(targetSampleRate, bitsPerSample, channels);

                    // 进行重采样
                    using (MediaFoundationResampler resampler = new MediaFoundationResampler(originalStream, targetFormat))
                    {
                        resampler.ResamplerQuality = 60; // 设置重采样质量

                        // 计算重采样后数据的大致长度
                        long estimatedLength = (long)(pcmData.Length * (double)targetSampleRate / originalSampleRate);
                        byte[] resampledData = new byte[estimatedLength];

                        int totalBytesRead = 0;
                        int bytesRead;
                        byte[] buffer = new byte[resampler.WaveFormat.AverageBytesPerSecond];
                        while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            Array.Copy(buffer, 0, resampledData, totalBytesRead, bytesRead);
                            totalBytesRead += bytesRead;
                        }

                        // 调整数组长度到实际读取的字节数
                        Array.Resize(ref resampledData, totalBytesRead);

                        return resampledData;
                    }
                }
            }
        }
        public void StartPlaying()
        {
            if (!IsPlaying)
            {
                _waveOut?.Play();
                IsPlaying = true;
            }
        }
        public void StopPlaying()
        {
            if (IsPlaying)
            {
                _waveOut?.Stop();
                IsPlaying = false;
            }
        }
        public void AddOutSamples(byte[] pcmData)
        {
            if (_waveOutProvider != null)
            {
                // 添加样本数据
                _waveOutProvider.AddSamples(pcmData, 0, pcmData.Length);
            }
        }
        public void AddOutSamples(float[] pcmData)
        {
            if (_waveOutProvider != null)
            {
                byte[] byteAudioData = FloatArrayToByteArray(pcmData);

                // 检查缓冲区可用空间
                while (_waveOutProvider.BufferedBytes + byteAudioData.Length > _waveOutProvider.BufferLength)
                {
                    // 等待一段时间，让缓冲区有足够空间
                    System.Threading.Thread.Sleep(10);
                }

                // 添加样本数据
                _waveOutProvider.AddSamples(byteAudioData, 0, byteAudioData.Length);
            }
        }
        private static byte[] FloatArrayToByteArray(float[] floatArray)
        {
            // 初始化一个与 float 数组长度两倍的 byte 数组，因为每个 short 占 2 个字节
            byte[] byteArray = new byte[floatArray.Length * 2];

            for (int i = 0; i < floatArray.Length; i++)
            {
                // 将 float 类型的值映射到 short 类型的范围
                short sample = (short)(floatArray[i] * short.MaxValue);

                // 将 short 类型的值拆分为两个字节
                byteArray[i * 2] = (byte)(sample & 0xFF);
                byteArray[i * 2 + 1] = (byte)(sample >> 8);
            }

            return byteArray;
        }
        private static float[] ByteArrayToFloatArray(byte[] byteArray)
        {
            // 检查字节数组的长度是否是 4 的倍数
            if (byteArray.Length % 4 != 0)
            {
                throw new ArgumentException("字节数组的长度必须是 4 的倍数。");
            }

            // 计算浮点数数组的长度
            int floatCount = byteArray.Length / 4;
            float[] floatArray = new float[floatCount];

            // 循环遍历字节数组，每次取 4 个字节转换为一个浮点数
            for (int i = 0; i < floatCount; i++)
            {
                floatArray[i] = BitConverter.ToSingle(byteArray, i * 4);
            }

            return floatArray;
        }
        public void Dispose()
        {
            IsPlaying = false;
            IsRecording = false;
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }

    }
}
