using System.Threading.Tasks;
using XiaoZhiSharp.Services;

namespace XiaoZhiSharp_MauiBlazorApp.Services
{
    public class AudioService : IDisposable, IAudioService
    {
        // 音频参数
        private const int SampleRate = 16000;
        private const int Bitrate = 16;
        private const int Channels = 1;
        private const int FrameDuration = 60;
        private const int FrameSize = SampleRate * FrameDuration / 1000; // 帧大小

        private bool _isPlaying;
        private bool _isRecording;

        public event IAudioService.PcmAudioEventHandler? OnPcmAudioEvent;

        public bool IsPlaying => _isPlaying;
        public bool IsRecording => _isRecording;
        public int VadCounter { get; private set; } = 0; // 用于语音活动检测的计数器
        public AudioService()
        {

        }





        public void StartRecording()
        {
            
        }

        public void StopRecording()
        {
           
        }

        /// <summary>
        /// 静音检测
        /// </summary>
        private bool IsAudioMute(byte[] buffer, int bytesRecorded)
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

        public void StartPlaying()
        {
           
        }

        public void StopPlaying()
        {
           
        }

        public void AddOutSamples(byte[] pcmData)
        {
           
        }

        public void AddOutSamples(float[] pcmData)
        {
            throw new NotImplementedException("不支持 float 类型的 PCM 数据");
        }

        public void Dispose()
        {
            
        }
    }
}