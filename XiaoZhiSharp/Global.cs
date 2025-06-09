using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp
{
    public class Global
    {
        public static bool IsDebug { get; set; } = true;
        public static bool IsAudio { get; set; } = true;
        public static int SampleRate_WaveOut { get; set; } = 24000;
        public static int SampleRate_WaveIn { get; set; } = 16000;
    }
}
