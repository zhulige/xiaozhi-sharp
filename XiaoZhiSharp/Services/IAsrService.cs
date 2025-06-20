using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public interface IAsrService
    {
        delegate Task TextEventHandler(string text);
        event TextEventHandler? OnTextEvent;
        void AddOutSamples(byte[] pcmData);
        void AddOutSamples(float[] pcmData);
    }
}
