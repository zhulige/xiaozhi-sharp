using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Services
{
    public interface IAudioService
    {
        delegate Task PcmAudioEventHandler(byte[] pcm);
        event PcmAudioEventHandler? OnPcmAudioEvent;
        bool IsPlaying { get; }
        bool IsRecording { get; }
        int VadCounter { get; }
        void StartRecording();
        void StopRecording();
        void StartPlaying();
        void StopPlaying();
        void AddOutSamples(byte[] pcmData);
        void AddOutSamples(float[] pcmData);
    }
}
