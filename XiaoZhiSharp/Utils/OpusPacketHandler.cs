using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    public class RtpHeader
    {
        public int Version { get; set; }
        public bool Padding { get; set; }
        public bool Extension { get; set; }
        public int CsrcCount { get; set; }
        public bool Marker { get; set; }
        public int PayloadType { get; set; }
        public ushort SequenceNumber { get; set; }  // 改为ushort
        public uint Timestamp { get; set; }
        public uint Ssrc { get; set; }
        public List<uint> CsrcIdentifiers { get; } = new(); // 新增CSRC列表
    }

    public class OpusPacketHandler
    {
        public static RtpHeader ReadRtpHeader(byte[] packet)
        {
            if (packet.Length < 12)
            {
                throw new ArgumentException("数据包长度不足，无法读取完整的 RTP 头部。");
            }

            RtpHeader header = new RtpHeader();

            // 第一个字节（第 0 字节）
            byte firstByte = packet[0];
            header.Version = (firstByte >> 6) & 0x03;
            header.Padding = ((firstByte >> 5) & 0x01) == 1;
            header.Extension = ((firstByte >> 4) & 0x01) == 1;
            header.CsrcCount = firstByte & 0x0F;

            // 第二个字节（第 1 字节）
            byte secondByte = packet[1];
            header.Marker = ((secondByte >> 7) & 0x01) == 1;
            header.PayloadType = secondByte & 0x7F;

            // 序列号（大端序）
            header.SequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(packet.AsSpan(2, 2));

            // 时间戳和SSRC
            header.Timestamp = BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(4, 4));
            header.Ssrc = BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(8, 4));

            // 处理CSRC列表
            for (int i = 0; i < header.CsrcCount; i++)
            {
                int offset = 12 + i * 4;
                if (offset + 4 > packet.Length) break;
                header.CsrcIdentifiers.Add(BinaryPrimitives.ReadUInt32BigEndian(packet.AsSpan(offset, 4)));
            }

            return header;
        }
    }
}
