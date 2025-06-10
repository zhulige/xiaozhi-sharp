using OpusSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using XiaoZhiSharp.Utils;

namespace XiaoZhiSharp.Services
{
    public class AudioOpusService
    {
        // Opus 相关组件
        private OpusDecoder opusDecoder;   // 解码器
        private OpusEncoder opusEncoder;   // 编码器
        private readonly object _lock = new();
        private int _currentSampleRate;
        private int _currentChannels;

        /// <summary>
        /// 编码器
        /// </summary>
        /// <param name="pcmData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="frameDuration"></param>
        /// <param name="bitrate"></param>
        /// <returns></returns>
        public byte[] Encode(byte[] pcmData, int sampleRate=24000, int channels=1, int frameDuration = 60, int bitrate = 16)
        {
            lock (_lock)
            {
                if (opusEncoder == null || _currentSampleRate != sampleRate || _currentChannels != channels)
                {
                    opusEncoder?.Dispose();
                    opusEncoder = new OpusEncoder(sampleRate, channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
                    _currentSampleRate = sampleRate;
                    _currentChannels = channels;
                }

                try
                {
                    // 计算帧大小 (采样数，不是字节数)
                    int frameSize = sampleRate * frameDuration / 1000; // 默认60ms帧

                    // 确保输入数据长度正确 (16位音频 = 2字节/样本)
                    int expectedBytes = frameSize * channels * 2;

                    if (pcmData.Length != expectedBytes)
                    {
                        // 调整数据长度或填充零
                        byte[] adjustedData = new byte[expectedBytes];
                        if (pcmData.Length < expectedBytes)
                        {
                            // 数据不足，复制现有数据并填充零
                            Array.Copy(pcmData, adjustedData, pcmData.Length);
                        }
                        else
                        {
                            // 数据过多，截断
                            Array.Copy(pcmData, adjustedData, expectedBytes);
                        }
                        pcmData = adjustedData;
                    }

                    short[] pcmShorts = new short[frameSize * channels];
                    for (int i = 0; i < pcmShorts.Length && i * 2 + 1 < pcmData.Length; i++)
                    {
                        pcmShorts[i] = BitConverter.ToInt16(pcmData, i * 2);
                    }

                    byte[] outputBuffer = new byte[4000]; // Opus最大包大小
                    int encodedLength = opusEncoder.Encode(pcmShorts, frameSize, outputBuffer, outputBuffer.Length);

                    byte[] result = new byte[encodedLength];
                    Array.Copy(outputBuffer, result, encodedLength);
                    return result;
                }
                catch (Exception ex)
                {
                    //LogConsole.WarningLine($"Opus 编码失败: {ex.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// 解码器
        /// </summary>
        /// <param name="opusData"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="frameDuration"></param>
        /// <param name="bitrate"></param>
        /// <returns></returns>
        public byte[] Decode(byte[] opusData, int sampleRate=24000, int channels=1, int frameDuration = 60, int bitrate = 16)
        {
            lock (_lock)
            {
                if (opusDecoder == null || _currentSampleRate != sampleRate || _currentChannels != channels)
                {
                    opusDecoder?.Dispose();
                    opusDecoder = new OpusDecoder(sampleRate, channels);
                    _currentSampleRate = sampleRate;
                    _currentChannels = channels;
                }
                try
                {
                    // 计算帧大小 (采样数，不是字节数)
                    int frameSize = sampleRate * frameDuration / 1000; // 默认60ms帧
                    short[] pcmShorts = new short[frameSize * channels];
                    int decodedSamples = opusDecoder.Decode(opusData, opusData.Length, pcmShorts, frameSize, false);
                    if (decodedSamples <= 0)
                        return Array.Empty<byte>();
                    byte[] pcmData = new byte[decodedSamples * 2 * channels];
                    for (int i = 0; i < decodedSamples * channels; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes(pcmShorts[i]);
                        Array.Copy(bytes, 0, pcmData, i * 2, 2);
                    }
                    return pcmData;
                }
                catch (Exception ex)
                {
                    //LogConsole.WarningLine($"Opus 解码失败: {ex.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// 将 PCM 数据转换为 float 数组
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public float[] ConvertByteToFloatPcm(byte[] byteArray)
        {
            int byteLength = byteArray.Length;
            int floatLength = byteLength / 2;
            float[] floatArray = new float[floatLength];

            for (int i = 0; i < floatLength; i++)
            {
                // 从 byte 数组中读取两个字节并转换为 short 类型
                short sample = BitConverter.ToInt16(byteArray, i * 2);
                // 将 short 类型的值转换为 float 类型，范围是 [-1, 1]
                floatArray[i] = sample / 32768.0f;
            }

            return floatArray;
        }
        /// <summary>
        /// 将 float 数组转换为 PCM 数据
        /// </summary>
        /// <param name="floatArray"></param>
        /// <returns></returns>
        public byte[] ConvertFloatToBytePcm(float[] floatArray)
        {
            int floatLength = floatArray.Length;
            byte[] byteArray = new byte[floatLength * 2];

            for (int i = 0; i < floatLength; i++)
            {
                // 将 float 类型的值转换为 short 类型
                short sample = (short)(floatArray[i] * 32767);
                // 将 short 类型的值转换为两个字节
                byte[] bytes = BitConverter.GetBytes(sample);
                // 将两个字节存储到 byte 数组中
                bytes.CopyTo(byteArray, i * 2);
            }

            return byteArray;
        }
    }
}
