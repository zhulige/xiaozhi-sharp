using PortAudioSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp.Services
{
    public class AudioPortService : IAudioService, IDisposable
    {
        // 音频输出相关组件
        private readonly PortAudioSharp.Stream? _waveOut;
        private readonly Queue<float[]> _waveOutStream = new Queue<float[]>();

        // 音频输入相关组件
        private readonly PortAudioSharp.Stream? _waveIn;

        public delegate Task PcmAudioEventHandler(byte[] pcm);
        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;

        // 音频参数
        private const int SampleRate = 24000;
        public int SampleRate_WaveIn { get; set; } = 16000;
        private const int Bitrate = 16;
        private const int Channels = 1;
        private const int FrameDuration = 60;
        private const int FrameSize = SampleRate * FrameDuration / 1000; // 帧大小

        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }
        public int VadCounter { get; private set; } = 0; // 用于语音活动检测的计数器
        public AudioPortService()
        {
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
                inParams: inparam, outParams: null, sampleRate: SampleRate_WaveIn, framesPerBuffer: 1440,
                streamFlags: StreamFlags.ClipOff, callback: InCallback, userData: IntPtr.Zero
            );

            // 启动音频播放
            StartPlaying();

            LogConsole.InfoLine($"当前默认音频输入设备： {inputDeviceIndex} ({inputInfo.name})");
            LogConsole.InfoLine($"当前默认音频输出设备： {outputDeviceIndex} ({outputInfo.name})");
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
                if (OnPcmAudioEvent != null)
                    OnPcmAudioEvent(buffer);

                return StreamCallbackResult.Continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StreamCallbackResult.Complete;
            }
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

        public void AddOutSamples(byte[] pcmData)
        {
            lock (_waveOutStream)
            {
                _waveOutStream.Enqueue(ByteArrayToFloatArray(pcmData));
            }
        }

        public void AddOutSamples(float[] pcmData)
        {
            lock (_waveOutStream)
            {
                _waveOutStream.Enqueue(pcmData);
            }
        }

        public void Dispose()
        {
            IsPlaying = false;
            IsRecording = false;
            _waveIn?.Dispose();
            _waveOut?.Dispose();
            PortAudio.Terminate();
        }
    }
}
