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
        private readonly OpusDecoder opusDecoder;   // 解码器
        private readonly OpusEncoder opusEncoder;   // 编码器

        // 音频参数
        public int SampleRate { get; set; } = 24000;
        public int Bitrate { get; set; } = 16;
        public int Channels { get; set; } = 1;
        public int FrameDuration { get; set; } = 60;
        public int FrameSize { get { 
            return SampleRate * FrameDuration / 1000; // 帧大小
            }  
        }
        // 计算帧的字节大小
        public int FrameByteSize => FrameSize * (Bitrate / 8);

        public AudioOpusService()
        {
            // 初始化 Opus 解码器
            opusDecoder = new OpusDecoder(SampleRate, Channels);
            // 初始化 Opus 编码器
            opusEncoder = new OpusEncoder(SampleRate, Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        }
        public AudioOpusService(int sampleRate, int bitrate=16, int channels = 1, int frameDuration = 60)
        {
            SampleRate = sampleRate;
            Bitrate = bitrate;
            Channels = channels;
            FrameDuration = frameDuration;

            // 初始化 Opus 解码器
            opusDecoder = new OpusDecoder(SampleRate, Channels);
            // 初始化 Opus 编码器
            opusEncoder = new OpusEncoder(SampleRate, Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="opusData"></param>
        /// <returns></returns>
        public byte[] ConvertOpusToPcm(byte[] opusData)
        {
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
                    return pcmBytes;
                }
            }
            catch (Exception ex)
            {
                LogConsole.WarningLine($"Opus 解码:{ex.Message}");
            }

            return new byte[0];
        }
        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="pcmData"></param>
        /// <returns></returns>
        public byte[] ConvertPcmToOpus(byte[] pcmData)
        {
            if (pcmData == null || pcmData.Length == 0)
                throw new ArgumentException("PCM数据不能为空", nameof(pcmData));

            // 验证数据长度
            if (pcmData.Length < FrameByteSize)
                throw new ArgumentException($"PCM数据长度 ({pcmData.Length}) 小于帧大小 ({FrameByteSize})", nameof(pcmData));

            // 转换为short数组（假设16位PCM）
            short[] frame = new short[FrameSize];
            Buffer.BlockCopy(pcmData, 0, frame, 0, FrameByteSize);

            // 创建输出缓冲区
            byte[] encodedFrame = new byte[FrameSize * 6];

            // 执行编码
            int encodedLength = opusEncoder.Encode(frame, frame.Length, encodedFrame, encodedFrame.Length);

            // 返回实际编码的数据
            byte[] result = new byte[encodedLength];
            Array.Copy(encodedFrame, result, encodedLength);

            return result;
        }
        //public List<byte[]> ConvertPcmToOpus(byte[] pcmData)
        //{
        //    List<byte[]> opusFrames = new List<byte[]>();
        //    try
        //    {
        //        // 确保输入数据长度是 FrameSize * 2 的整数倍（因为每个样本是 2 字节）
        //        int remainder = pcmData.Length % (FrameSize * 2);
        //        if (remainder != 0)
        //        {
        //            // 填充零以确保数据长度是 FrameSize * 2 的整数倍
        //            Array.Resize(ref pcmData, pcmData.Length + (FrameSize * 2 - remainder));
        //        }

        //        // 将字节数组转换为 short 数组
        //        short[] pcmShortData = new short[pcmData.Length / 2];
        //        Buffer.BlockCopy(pcmData, 0, pcmShortData, 0, pcmData.Length);

        //        // 逐帧编码
        //        for (int i = 0; i < pcmShortData.Length; i += FrameSize)
        //        {
        //            short[] frame = new short[FrameSize];
        //            Array.Copy(pcmShortData, i, frame, 0, FrameSize);

        //            // 进行编码
        //            byte[] encodedFrame = new byte[FrameSize * 6];
        //            int encodedLength = opusEncoder.Encode(frame, frame.Length, encodedFrame, encodedFrame.Length);

        //            if (encodedLength > 0)
        //            {
        //                byte[] trimmedFrame = new byte[encodedLength];
        //                Array.Copy(encodedFrame, 0, trimmedFrame, 0, encodedLength);
        //                opusFrames.Add(trimmedFrame);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogConsole.WarningLine($"Opus 编码:{ex.Message}");
        //    }

        //    return opusFrames;
        //}
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
