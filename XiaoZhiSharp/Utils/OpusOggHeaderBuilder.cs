using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class OpusOggHeaderBuilder
{
    public static byte[] GenerateOggPageHeader(long granulePosition, int bitstreamSerialNumber, int pageSequenceNumber, byte headerType, byte[] segmentTable, byte[] pageData)
    {
        byte[] header = new byte[27 + segmentTable.Length];
        Array.Copy(Encoding.ASCII.GetBytes("OggS"), 0, header, 0, 4);
        header[4] = 0; // version
        header[5] = headerType;
        Array.Copy(BitConverter.GetBytes(granulePosition), 0, header, 6, 8);
        Array.Copy(BitConverter.GetBytes(bitstreamSerialNumber), 0, header, 14, 4);
        Array.Copy(BitConverter.GetBytes(pageSequenceNumber), 0, header, 18, 4);
        Array.Clear(header, 22, 4); // checksum placeholder
        header[26] = (byte)segmentTable.Length;
        Array.Copy(segmentTable, 0, header, 27, segmentTable.Length);

        // 合并页头和页数据
        byte[] dataToChecksum = new byte[header.Length + pageData.Length];
        Array.Copy(header, 0, dataToChecksum, 0, header.Length);
        Array.Copy(pageData, 0, dataToChecksum, header.Length, pageData.Length);

        uint crc = CalculateChecksum(dataToChecksum);
        Array.Copy(BitConverter.GetBytes(crc), 0, header, 22, 4);
        return header;
    }

    public static byte[] GenerateOpusIdentificationHeader(int sampleRate, int channels)
    {
        byte[] header = new byte[19];
        Array.Copy(Encoding.ASCII.GetBytes("OpusHead"), 0, header, 0, 8);
        header[8] = 1; // version
        header[9] = (byte)channels;
        Array.Copy(BitConverter.GetBytes(3840), 0, header, 10, 2);
        Array.Copy(BitConverter.GetBytes(sampleRate), 0, header, 12, 4);
        Array.Copy(BitConverter.GetBytes(0), 0, header, 16, 2);
        header[18] = 0; // mapping family
        return header;
    }

    public static byte[] GenerateOpusCommentHeader()
    {
        byte[] header = new byte[16];
        Array.Copy(Encoding.ASCII.GetBytes("OpusTags"), 0, header, 0, 8);
        Array.Copy(BitConverter.GetBytes(0), 0, header, 8, 4); // vendor length
        Array.Copy(BitConverter.GetBytes(0), 0, header, 12, 4); // comment count
        return header;
    }

    private static uint CalculateChecksum(byte[] data)
    {
        uint crc = 0;
        uint[] crcTable = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint r = i << 24;
            for (int j = 0; j < 8; j++)
            {
                r = (r & 0x80000000) != 0 ? (r << 1) ^ 0x04c11db7 : r << 1;
            }
            crcTable[i] = r;
        }

        foreach (byte b in data)
        {
            crc = (crc << 8) ^ crcTable[((crc >> 24) ^ b) & 0xFF];
        }

        return crc;
    }

    public static byte[] CreateSegmentTable(byte[] packet)
    {
        List<byte> segmentTable = new List<byte>();
        int remainingLength = packet.Length;
        while (remainingLength > 0)
        {
            if (remainingLength >= 255)
            {
                segmentTable.Add(255);
                remainingLength -= 255;
            }
            else
            {
                segmentTable.Add((byte)remainingLength);
                remainingLength = 0;
            }
        }
        return segmentTable.ToArray();
    }
}