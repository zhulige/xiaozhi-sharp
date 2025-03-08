using NAudio.Wave;
using OpusSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp.Pools;

namespace XiaoZhiSharp.Services
{
    public class AudioService : IDisposable
    {
        // Opus 相关组件
        private readonly OpusDecoder opusDecoder;   // 解码器
        private readonly OpusEncoder opusEncoder;   // 编码器
        // NAudio 音频输出相关组件
        private readonly IWavePlayer? _waveOut;
        private readonly BufferedWaveProvider? _waveOutProvider = null;
        // NAudio 音频输入相关组件
        private readonly WaveInEvent? _waveIn;
        private readonly BufferedWaveProvider? _waveInProvider = null;

        // 音频参数
        private const int SampleRate = 16000;
        private const int Bitrate = 16;
        private const int Channels = 1;
        private const int FrameDuration = 60;
        private const int FrameSize = SampleRate * FrameDuration / 1000; // 帧大小

        // Opus 数据包缓存池
        public readonly PacketCachePool _opusPackets = new PacketCachePool(1000);
        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }

        public AudioService()
        {

            // 初始化 Opus 解码器
            opusDecoder = new OpusDecoder(SampleRate, Channels);
            // 初始化 Opus 编码器
            opusEncoder = new OpusEncoder(SampleRate, Channels, OpusPredefinedValues.OPUS_APPLICATION_AUDIO);
            // 初始化音频输出相关组件
            var waveFormat = new WaveFormat(SampleRate, 16, Channels);
            _waveOut = new WaveOutEvent();
            _waveOutProvider = new BufferedWaveProvider(waveFormat);
            _waveOut.Init(_waveOutProvider);

            // 初始化音频输入相关组件
            _waveIn = new WaveInEvent();
            _waveIn.WaveFormat = new WaveFormat(48000, 16, 1);
            _waveIn.BufferMilliseconds = 60;
            _waveIn.DataAvailable += WaveIn_DataAvailable;

            // 启动音频播放线程
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    StartPlaying();
                    while (IsPlaying)
                    {
                        // 可以添加更多逻辑，如缓冲区检查等
                        Thread.Sleep(10);
                    }
                    StopPlaying();
                }
            });
            thread.Start();

        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
        {
            bool isMute = IsAudioMute(e.Buffer, e.BytesRecorded);
            if (isMute)
            {
                Console.WriteLine("音频处于静音状态");
            }
            else
            {
                //Console.WriteLine("音频有声音");
                //录音 PCM 48000 16 1
                byte[] pcmBytes48000 = e.Buffer;
                //int frameSize = 48000 * FrameDuration / 1000;
                //Console.WriteLine($"PCM Data Length: {pcmBytes.Length}");
                byte[] pcmBytes16000 = ConvertPcmSampleRate(pcmBytes48000, 48000, 16000, 1, 16);

                //测试播放录音
                //waveOutProvider.AddSamples(pcmBytes16000, 0, pcmBytes16000.Length);
                ProcessAudioData(pcmBytes16000, pcmBytes16000.Length);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="bytesRecorded"></param>
        private void ProcessAudioData(byte[] buffer, int bytesRecorded)
        {
            int frameCount = bytesRecorded / (FrameSize * 2); // 每个样本 2 字节

            for (int i = 0; i < frameCount; i++)
            {
                byte[] frame = new byte[FrameSize * 2];
                Array.Copy(buffer, i * FrameSize * 2, frame, 0, FrameSize * 2);

                // 编码音频帧
                byte[] opusBytes = new byte[960];
                int encodedLength = opusEncoder.Encode(frame, FrameSize, opusBytes, opusBytes.Length);

                byte[] opusPacket = new byte[encodedLength];
                Array.Copy(opusBytes, 0, opusPacket, 0, encodedLength);
                _opusPackets.AddPacketAsync(opusPacket);

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

        /// <summary>
        /// Pcm 重采样
        /// </summary>
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

        public void StartRecording()
        {
            if (!IsRecording)
            {
                _waveIn?.StartRecording();
                IsRecording = true;
            }
        }

        public void StopRecording()
        {
            if (IsRecording)
            {
                _waveIn?.StopRecording();
                IsRecording = false;
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

        public void ReceiveOpusData(byte[] opusData)
        {
            if (opusData == null || opusData.Length == 0)
                return;

            try
            {
                // 解码 Opus 数据
                short[] pcmData = new short[FrameSize * 2];
                int decodedSamples = opusDecoder.Decode(opusData, opusData.Length, pcmData, FrameSize, false);
                if (decodedSamples > 0)
                {
                    // 将解码后的 PCM 数据转换为字节数组
                    byte[] pcmBytes = new byte[decodedSamples * 2];
                    Buffer.BlockCopy(pcmData, 0, pcmBytes, 0, pcmBytes.Length);

                    // 将 PCM 数据添加到缓冲区
                    if (_waveOutProvider != null)
                        _waveOutProvider.AddSamples(pcmBytes, 0, pcmBytes.Length);
                }

            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Error decoding Opus data: {ex.Message}");
            }

        }

        public byte[] ConvertOpusToPcm(byte[] opusData, int sampleRate, int channels)
        {

            // 计算最大可能的 PCM 数据长度
            int maxPcmLength = 1024;
            short[] pcmData = new short[maxPcmLength];

            // 解码 Opus 数据为 PCM 数据
            int decodedSamples = opusDecoder.Decode(opusData, opusData.Length, pcmData, maxPcmLength, false);

            // 将 short 数组转换为 byte 数组
            byte[] pcmBytes = new byte[decodedSamples * channels * sizeof(short)];
            Buffer.BlockCopy(pcmData, 0, pcmBytes, 0, pcmBytes.Length);

            return pcmBytes;

        }

        public void Dispose()
        {
            IsPlaying = false;
            IsRecording = false;
            opusDecoder?.Dispose();
            opusEncoder?.Dispose();
            _waveIn?.Dispose();
            _waveOut?.Dispose();
        }
    }
}
