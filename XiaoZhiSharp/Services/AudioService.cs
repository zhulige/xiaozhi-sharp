using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using PortAudioSharp;
using OpusSharp.Core;
using XiaoZhiSharp.Utils;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace XiaoZhiSharp.Services
{
    public class AudioService : IDisposable
    {
        // Opus 相关组件
        private readonly OpusDecoder opusDecoder;   // 解码器
        private readonly OpusEncoder opusEncoder;   // 编码器
        // 音频输出相关组件
        private readonly PortAudioSharp.Stream? _waveOut;
        //private readonly Queue<float[]> _waveOutStream = new Queue<float[]>();
        private readonly ConcurrentQueue<float[]> _waveOutStream = new ConcurrentQueue<float[]>();

        // 音频输入相关组件
        private readonly PortAudioSharp.Stream? _waveIn;

        // 音频参数
        private const int SampleRate = 24000;
        private const int Channels = 1;
        private const int FrameDuration = 60;
        private const int FrameSize = SampleRate * FrameDuration / 1000; // 帧大小

        // Opus 数据包缓存池
        private readonly Queue<byte[]> _opusRecordPackets = new Queue<byte[]>();
        private readonly Queue<byte[]> _opusPlayPackets = new Queue<byte[]>();

        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }

        public AudioService()
        {
            // 初始化 Opus 解码器和编码器
            opusDecoder = new OpusDecoder(SampleRate, Channels);
            opusEncoder = new OpusEncoder(SampleRate, Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            //opusEncoder = new OpusEncoder(SampleRate, Channels,OpusSharp.Core.Enums.PreDefCtl.OPUS_APPLICATION_VOIP);

            // 初始化音频输出组件
            PortAudio.Initialize();
            int outputDeviceIndex = PortAudio.DefaultOutputDevice;
            if (outputDeviceIndex == PortAudio.NoDevice)
            {
                Console.WriteLine("No default output device found");
                LogConsole.InfoLine(PortAudio.VersionInfo.versionText);
                LogConsole.WriteLine($"Number of devices: {PortAudio.DeviceCount}");
                for (int i = 0; i != PortAudio.DeviceCount; ++i)
                {
                    LogConsole.WriteLine($" Device {i}");
                    DeviceInfo deviceInfo = PortAudio.GetDeviceInfo(i);
                    LogConsole.WriteLine($"   Name: {deviceInfo.name}");
                    LogConsole.WriteLine($"   Max input channels: {deviceInfo.maxInputChannels}");
                    LogConsole.WriteLine($"   Default sample rate: {deviceInfo.defaultSampleRate}");
                }
                //Environment.Exit(1);
            }
            var outputInfo = PortAudio.GetDeviceInfo(outputDeviceIndex);
            var outparam = new StreamParameters
            {
                device = outputDeviceIndex,
                channelCount = Channels,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = outputInfo.defaultLowOutputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero
            };

            _waveOut = new PortAudioSharp.Stream(
                inParams: null, outParams: outparam, sampleRate: SampleRate, framesPerBuffer: 1440,
                streamFlags: StreamFlags.ClipOff, callback: PlayCallback, userData: IntPtr.Zero
            );

            // 初始化音频输入组件
            int inputDeviceIndex = PortAudio.DefaultInputDevice;
            if (inputDeviceIndex == PortAudio.NoDevice)
            {
                Console.WriteLine("No default input device found");
                //Environment.Exit(1);
            }
            var inputInfo = PortAudio.GetDeviceInfo(inputDeviceIndex);
            var inparam = new StreamParameters
            {
                device = inputDeviceIndex,
                channelCount = Channels,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = inputInfo.defaultLowInputLatency,
                hostApiSpecificStreamInfo = IntPtr.Zero
            };

            _waveIn = new PortAudioSharp.Stream(
                inParams: inparam, outParams: null, sampleRate: SampleRate, framesPerBuffer: 1440,
                streamFlags: StreamFlags.ClipOff, callback: InCallback, userData: IntPtr.Zero
            );

            // 启动音频播放
            StartPlaying();

            // 启动 Opus 数据解码线程
            Thread threadOpus = new Thread(() =>
            {
                while (true)
                {
                    if (_opusPlayPackets.TryDequeue(out var opusData))
                    {
                        AddOutStreamSamples(opusData);
                    }
                    Thread.Sleep(1);
                }
            });
            threadOpus.Start();

            LogConsole.InfoLine($"当前默认音频输入设备： {inputDeviceIndex} ({inputInfo.name})");
            LogConsole.InfoLine($"当前默认音频输出设备 {outputDeviceIndex} ({outputInfo.name})");
        }

        private StreamCallbackResult PlayCallback(
            IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags, IntPtr userData)
        {
            if (_waveOutStream.Count <= 0)
            {
                //return StreamCallbackResult.Complete;
            }
            try
            {
                while (_waveOutStream.Count > 0)
                {
                    float[]? buffer;
                    lock (_waveOutStream)
                    {
                        if (_waveOutStream.TryDequeue(out buffer))
                        {
                            if (buffer.Length < frameCount)
                            {
                                float[] paddedBuffer = new float[frameCount];
                                Array.Copy(buffer, paddedBuffer, buffer.Length);
                                Marshal.Copy(paddedBuffer, 0, output, (int)frameCount);
                                //Thread.Sleep(10);
                            }
                            else
                            {
                                Marshal.Copy(buffer, 0, output, (int)frameCount);
                            }
                        }
                        return StreamCallbackResult.Continue;
                    }
                }
                return StreamCallbackResult.Continue;
                //return StreamCallbackResult.Complete;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StreamCallbackResult.Complete;
            }
        }

        private StreamCallbackResult InCallback(
            IntPtr input, IntPtr output, uint frameCount, ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags, IntPtr userData)
        {
            try
            {
                if (!IsRecording)
                {
                    return StreamCallbackResult.Complete;
                }

                // 创建一个数组来存储输入的音频数据
                float[] samples = new float[frameCount];
                // 将输入的音频数据从非托管内存复制到托管数组
                Marshal.Copy(input, samples, 0, (int)frameCount);

                // 将音频数据转换为字节数组
                byte[] buffer = FloatArrayToByteArray(samples);

                // 处理音频数据
                AddRecordSamples(buffer, buffer.Length);

                return StreamCallbackResult.Continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StreamCallbackResult.Complete;
            }
        }

        private void AddRecordSamples(byte[] buffer, int bytesRecorded)
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
                _opusRecordPackets.Enqueue(opusPacket);
                //_opusPlayPackets.Enqueue(opusPacket);
            }
        }

        public void AddOutStreamSamples(byte[] opusData)
        {
            if (opusData == null || opusData.Length == 0)
                return;

            //Utils.RtpHeader rh = OpusPacketHandler.ReadRtpHeader(opusData);
            //LogConsole.ReceiveLine(JsonConvert.SerializeObject(rh));
            //if (rh.SequenceNumber == 1993 || rh.Marker==false)
            //    return;

            try
            {
                // 解码 Opus 数据
                short[] pcmData = new short[FrameSize * 10];
                int decodedSamples = opusDecoder.Decode(opusData, opusData.Length, pcmData, FrameSize * 10, false);

                if (decodedSamples > 0)
                {
                    // 将解码后的 PCM 数据转换为 float 数组
                    float[] floatData = new float[decodedSamples];
                    for (int i = 0; i < decodedSamples; i++)
                    {
                        floatData[i] = pcmData[i] / (float)short.MaxValue;
                    }

                    // 将 PCM 数据添加到缓冲区
                    lock (_waveOutStream)
                    {
                        _waveOutStream.Enqueue(floatData);
                    }

                    if (_waveOutStream.Count > 5)
                    {
                        if (!IsPlaying)
                        {
                            StartPlaying();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogConsole.ErrorLine($"Error decoding Opus data: {ex.Message}");
                //LogConsole.ReceiveLine(JsonConvert.SerializeObject(rh));
            }
        }

        public void StartRecording()
        {
            if (!IsRecording)
            {
                _waveIn?.Start();
                IsRecording = true;
            }
        }

        public void StopRecording()
        {
            if (IsRecording)
            {
                _waveIn?.Stop();
                IsRecording = false;
            }
        }

        public void StartPlaying()
        {
            if (!IsPlaying)
            {
                _waveOut?.Start();
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

        public void OpusPlayEnqueue(byte[] opusData)
        {
            _opusPlayPackets.Enqueue(opusData);
        }

        public bool OpusRecordEnqueue(out byte[]? opusData)
        {
            return _opusRecordPackets.TryDequeue(out opusData);
        }

        public static byte[] FloatArrayToByteArray(float[] floatArray)
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

        public static float[] ByteArrayToFloatArray(byte[] byteArray)
        {
            int floatArrayLength = byteArray.Length / 2;
            float[] floatArray = new float[floatArrayLength];

            for (int i = 0; i < floatArrayLength; i++)
            {
                floatArray[i] = BitConverter.ToInt16(byteArray, i * 2) / 32768f;
            }

            return floatArray;
        }

        public void Dispose()
        {
            IsPlaying = false;
            IsRecording = false;
            opusDecoder?.Dispose();
            opusEncoder?.Dispose();
            _waveIn?.Dispose();
            _waveOut?.Dispose();
            PortAudio.Terminate();
        }
    }
}
