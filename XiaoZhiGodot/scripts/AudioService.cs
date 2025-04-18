using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using OpusSharp.Core;

namespace AI_
{
    public partial class AudioService : Node, IDisposable
    {
        // 加载原生库
        [DllImport("opus", EntryPoint = "opus_get_version_string")]
        private static extern IntPtr OpusGetVersionString();

        // Opus 相关组件
        private readonly OpusDecoder decoder;   // 解码器
        private readonly OpusEncoder encoder;   // 编码器
        private AudioStreamPlayer audioPlayer;      // 音频播放器
        private AudioStreamWav audioStream;         // 音频流
        private Thread opusThread;                  // Opus处理线程
        private bool isRunning = true;              // 线程运行标志
        
        // 音频参数
        private const int EncodeSampleRate = 16000;  // 编码采样率（发送到服务端）
        private const int DecodeSampleRate = 24000;  // 解码采样率（从服务端接收）
        private const int Channels = 1;
        private const int FrameDuration = 60;  // 帧持续时间（毫秒）
        // 根据采样率和帧持续时间计算帧大小
        private const int DecodeFrameSize = DecodeSampleRate * FrameDuration / 1000; // 解码帧大小 (24000 * 60 / 1000 = 1440)
        private const int EncodeFrameSize = EncodeSampleRate * FrameDuration / 1000; // 编码帧大小 (16000 * 60 / 1000 = 960)
        // Opus 数据包缓存池
        private readonly Queue<byte[]> _opusRecordPackets = new Queue<byte[]>();
        private readonly Queue<byte[]> _opusPlayPackets = new Queue<byte[]>();
        
        // 缓冲区参数
        private const int OptimalBufferSize = 1024*512; // 512kb 最佳缓冲大小
        private const int PlaybackThreshold = 1024 * 8; // 8KB 开始播放的阈值
        
        private readonly Queue<byte[]> playbackQueue = new Queue<byte[]>();
        private byte[] currentPlaybackBuffer = new byte[0];
        private bool isLastPacket = false;
        private DateTime lastPacketTime = DateTime.Now;
        private const int PacketTimeout = 500; // 500ms 超时时间

        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }

        public AudioService(AudioStreamPlayer audioPlayer)
        {
            Name = "AudioService"; // 设置节点名称
            // 保存传入的音频播放器
            this.audioPlayer = audioPlayer;
            
            try
            {
                // 尝试加载opus库
                var version = Marshal.PtrToStringAnsi(OpusGetVersionString());
                GD.Print($"Opus version: {version}");
                
                // 初始化 Opus 解码器和编码器
                decoder = new OpusDecoder(DecodeSampleRate, Channels);
                encoder = new OpusEncoder(DecodeSampleRate, Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
                

                // 初始化音频流
                audioStream = new AudioStreamWav();
                audioStream.Format = AudioStreamWav.FormatEnum.Format16Bits;
                audioStream.Stereo = Channels == 2;
                audioStream.MixRate = DecodeSampleRate;  // 使用24000Hz播放
                this.audioPlayer.Stream = audioStream;

                // 启动 Opus 数据解码线程
                opusThread = new Thread(ProcessOpusData);
                opusThread.Start();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to initialize Opus: {ex.Message}");
                throw;
            }
        }

        //Opus 数据解码线程
        private void ProcessOpusData()
        {
            while (isRunning)
            {
                try
                {
                    if (_opusPlayPackets.Count > 0)
                    {
                        byte[] opusData;
                        lock (_opusPlayPackets)
                        {
                            opusData = _opusPlayPackets.Dequeue();
                        }
                        if (opusData != null)
                        {
                            AddOutStreamSamples(opusData);
                        }
                    }
                   // Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error in ProcessOpusData: {ex.Message}");
                }
            }
        }
        
        //服务端opus数据进行解码ToPCM
        public void AddOutStreamSamples(byte[] opusData)
        {
            if (opusData == null || opusData.Length == 0 || audioPlayer == null || audioStream == null)
                return;

            try
            {
                // 解码 Opus 数据
                short[] pcmData = new short[DecodeFrameSize * 10];
                int decodedSamples = decoder.Decode(opusData, opusData.Length, pcmData, DecodeFrameSize * 10, false);

                if (decodedSamples > 0)
                {
                    // 将short数组转换为byte数组，确保使用小端序
                    byte[] pcmBytes = new byte[decodedSamples * 2];
                    for (int i = 0; i < decodedSamples; i++)
                    {
                        short sample = pcmData[i];
                        byte[] bytes = BitConverter.GetBytes(sample);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(bytes);
                        }
                        pcmBytes[i * 2] = bytes[0];
                        pcmBytes[i * 2 + 1] = bytes[1];
                    }

                    // 更新最后数据包时间
                    lastPacketTime = DateTime.Now;
                    
                    // 添加到播放队列
                    lock (playbackQueue)
                    {
                        playbackQueue.Enqueue(pcmBytes);
                    }

                    // 检查并更新播放状态
                   CallDeferred( nameof(UpdatePlaybackState));
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error decoding Opus data: {ex.Message}");
            }
        }
        
        private void UpdatePlaybackState()
        {
            try
            {
                lock (playbackQueue)
                {
                    // 计算当前缓冲区大小
                    int totalBuffered = currentPlaybackBuffer.Length + 
                                      playbackQueue.Sum(x => x?.Length ?? 0);

                    // 检查是否需要开始播放
                    if (!audioPlayer.Playing && totalBuffered >= PlaybackThreshold)
                    {
                        StartPlayback();
                    }
                    // 检查是否需要更新音频数据
                    else if (audioPlayer.Playing && 
                            (totalBuffered >= OptimalBufferSize || 
                             (DateTime.Now - lastPacketTime).TotalMilliseconds > PacketTimeout))
                    {
                        UpdateAudioData();
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新播放状态时出错: {ex.Message}");
            }
        }

        private void StartPlayback()
        {
            try
            {
                UpdateAudioData();
                if (currentPlaybackBuffer.Length > 0)
                {
                    audioStream.Data = currentPlaybackBuffer;
                    audioPlayer.Stream = audioStream;
                    audioPlayer.Play();
                    //GD.Print($"开始播放，缓冲区大小: {currentPlaybackBuffer.Length}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"开始播放时出错: {ex.Message}");
            }
        }

        private void UpdateAudioData()
        {
            try
            {
                lock (playbackQueue)
                {
                    // 合并队列中的数据
                    List<byte> newBuffer = new List<byte>();
                    while (playbackQueue.Count > 0)
                    {
                        var data = playbackQueue.Dequeue();
                        if (data != null)
                        {
                            newBuffer.AddRange(data);
                        }
                    }

                    // 如果有新数据
                    if (newBuffer.Count > 0)
                    {
                        currentPlaybackBuffer = newBuffer.ToArray();
                        // 使用CallDeferred在主线程中更新音频流
                        //CallDeferred(nameof(UpdateAudioStream), currentPlaybackBuffer);
                        UpdateAudioStream(currentPlaybackBuffer);
                        //GD.Print($"更新音频数据，大小: {currentPlaybackBuffer.Length}");
                    }
                    // 如果没有新数据且超时，停止播放
                    else if ((DateTime.Now - lastPacketTime).TotalMilliseconds > PacketTimeout)
                    {
                       // CallDeferred(nameof(StopPlayback));
                        StopPlayback();
                        currentPlaybackBuffer = new byte[0];
                        GD.Print("播放超时，停止播放");
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新音频数据时出错: {ex.Message}");
            }
        }

        private void UpdateAudioStream(byte[] data)
        {
            try
            {
                if (audioStream != null && audioPlayer != null && data != null && data.Length > 0)
                {
                    audioStream.Data = data;
                    audioPlayer.Stream = audioStream;

                    if (!audioPlayer.Playing)
                    {
                        audioPlayer.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"更新音频流时出错: {ex.Message}");
            }
        }

        private void StopPlayback()
        {
            try
            {
                if (audioPlayer != null)
                {
                    audioPlayer.Stop();
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"停止播放时出错: {ex.Message}");
            }
        }

        public void StartRecording()
        {
            if (!IsRecording)
            {
                // 这里应调用 Godot 的录音接口
                IsRecording = true;
            }
        }

        public void StopRecording()
        {
            if (IsRecording)
            {
                // 这里应调用 Godot 的录音停止接口
                IsRecording = false;
            }
        }
        
        public void StopPlaying()
        {
            if (IsPlaying && audioPlayer != null)
            {
                IsPlaying = false;
                audioPlayer.Stop();
            }
        }

        //录音采用转换
        private byte[] ConvertPcmSampleRate(byte[] pcmData, int originalSampleRate, int targetSampleRate, int channels, int bitsPerSample)
        {
            try
            {
                // 将字节数组转换为short数组
                short[] inputData = new short[pcmData.Length / 2];
                Buffer.BlockCopy(pcmData, 0, inputData, 0, pcmData.Length);

                // 计算输出长度
                int outputLength = (int)((long)inputData.Length * targetSampleRate / originalSampleRate);
                short[] outputData = new short[outputLength];

                // 使用线性插值进行重采样
                double ratio = (double)originalSampleRate / targetSampleRate;
                for (int i = 0; i < outputLength; i++)
                {
                    double pos = i * ratio;
                    int index = (int)pos;
                    double fraction = pos - index;

                    if (index >= inputData.Length - 1)
                    {
                        outputData[i] = inputData[inputData.Length - 1];
                    }
                    else
                    {
                        // 线性插值
                        short sample1 = inputData[index];
                        short sample2 = inputData[index + 1];
                        outputData[i] = (short)(sample1 * (1 - fraction) + sample2 * fraction);
                    }
                }

                // 将short数组转换回byte数组
                byte[] outputBytes = new byte[outputLength * 2];
                Buffer.BlockCopy(outputData, 0, outputBytes, 0, outputBytes.Length);

                return outputBytes;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"重采样时出错: {ex.Message}");
                return pcmData; // 出错时返回原始数据
            }
        }
        private bool IsAudioMute(byte[] pcmData, int length)
        {
            try
            {
                if (pcmData == null || length <= 0)
                    return true;

                // 确保数据长度是2的倍数（16位PCM）
                if (length % 2 != 0)
                {
                    length = length - 1;
                }

                // 将字节数组转换为short数组
                short[] samples = new short[length / 2];
                Buffer.BlockCopy(pcmData, 0, samples, 0, length);

                // 计算音频数据的平均音量
                double sum = 0;
                int count = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    // 只计算绝对值大于10的样本，避免背景噪音干扰
                    if (Math.Abs(samples[i]) > 10)
                    {
                        sum += Math.Abs(samples[i]);
                        count++;
                    }
                }

                // 如果没有有效样本，认为是静音
                if (count == 0)
                    return true;

                double average = sum / count;
                
                // 调整静音阈值，原来的100太敏感了
                bool isMute = average < 10; // 降低阈值到10
                
                if (isMute)
                {
                    GD.Print($"静音检查: 平均音量 = {average}, 有效样本数 = {count}");
                }
                
                return isMute;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"静音检查出错: {ex.Message}");
                return false; // 出错时默认不是静音
            }
        }
        
        //录音opus压缩
        public void AddRecordSamples(byte[] buffer, int bytesRecorded)
        {
            try
            {
                // 检查是否是静音
               // if (IsAudioMute(buffer, bytesRecorded))
               // {
               //     GD.Print("静音检查");
               //     return;
               // }

                // 重采样：从48000Hz到16000Hz
                var resampledData = ConvertPcmSampleRate(buffer, 48000, 16000, 1, 16);
                //GD.Print($"重采样后数据长度: {resampledData.Length}");

                // 将字节数组转换为short数组
                short[] pcmData = new short[resampledData.Length / 2];
                Buffer.BlockCopy(resampledData, 0, pcmData, 0, resampledData.Length);

                // 计算帧数
                int frameCount = pcmData.Length / EncodeFrameSize;
                //GD.Print($"帧数: {frameCount}");

                for (int i = 0; i < frameCount; i++)
                {
                    // 准备编码数据
                    short[] frame = new short[EncodeFrameSize];
                    Array.Copy(pcmData, i * EncodeFrameSize, frame, 0, EncodeFrameSize);

                    // 编码音频帧
                    byte[] opusBytes = new byte[960]; // 最大Opus帧大小
                    int encodedLength = encoder.Encode(frame, EncodeFrameSize, opusBytes, opusBytes.Length);

                    if (encodedLength > 0)
                    {
                        byte[] opusPacket = new byte[encodedLength];
                        Array.Copy(opusBytes, 0, opusPacket, 0, encodedLength);
                        OpusRecordEnqueue(opusPacket);
                        //GD.Print($"编码帧大小: {encodedLength}");
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"处理音频样本时出错: {ex.Message}");
            }
        }
        
        public void OpusPlayEnqueue(byte[] opusData)
        {
            //GD.Print("opus数据进入_opusPlayPackets");
            _opusPlayPackets.Enqueue(opusData);
        }

        public bool OpusRecordTryDequeuee(out byte[]? opusData)
        {
            if (_opusRecordPackets.Count > 0)
            {
                opusData = _opusRecordPackets.Dequeue();
                return true;
            }
            opusData = null;
            return false;
        }
        
        public void OpusRecordEnqueue(byte[]? opusData)
        {
             _opusRecordPackets.Enqueue(opusData);
        }
        
        
        public void Dispose()
        {
            // 停止线程
            isRunning = false;
            opusThread?.Join(1000); // 等待线程结束，最多等待1秒
            
            // 停止播放
            StopPlaying();
            IsRecording = false;
            
            // 释放资源
            decoder?.Dispose();
            encoder?.Dispose();
            
            // 清理音频流
            if (audioStream != null)
            {
                audioStream.Data = new byte[0]; // 清空音频数据
            }
            
            // 不要释放audioPlayer，它由外部管理
        }
        
    }
}

