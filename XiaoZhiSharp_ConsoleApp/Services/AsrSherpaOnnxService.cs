using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiaoZhiSharp.Services;

namespace XiaoZhiSharp_ConsoleApp.Services
{
    public class AsrSherpaOnnxService : IAsrService, IDisposable
    {
        public event IAsrService.TextEventHandler? OnTextEvent;

        public void AddOutSamples(byte[] pcmData)
        {
            throw new NotImplementedException();
        }

        public void AddOutSamples(float[] pcmData)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
