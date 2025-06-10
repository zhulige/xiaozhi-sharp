using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    public class AudioHelper
    {
        /// <summary>
        /// 静音检测
        /// </summary>
        public static bool IsAudioMute(byte[] buffer, int bytesRecorded)
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
    }
}
