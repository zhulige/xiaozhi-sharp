using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Pools
{
    // 定义一个包含数据包和序列号的结构体
    public struct PacketWithSequence
    {
        public byte[] Packet;
        public int SequenceNumber;

        public PacketWithSequence(byte[] packet, int sequenceNumber)
        {
            Packet = packet;
            SequenceNumber = sequenceNumber;
        }
    }

    public class PacketCachePool
    {
        private readonly Queue<PacketWithSequence> _packetQueue;
        private readonly int _maxCapacity;
        private readonly SemaphoreSlim _semaphore;
        private int _sequenceCounter;

        public PacketCachePool(int maxCapacity)
        {
            _packetQueue = new Queue<PacketWithSequence>();
            _maxCapacity = maxCapacity;
            _semaphore = new SemaphoreSlim(maxCapacity);
            _sequenceCounter = 0;
        }

        // 添加分包到缓存池
        public async Task AddPacketAsync(byte[] packet)
        {
            await _semaphore.WaitAsync();
            lock (_packetQueue)
            {
                // 为数据包分配序列号并添加到队列
                var packetWithSequence = new PacketWithSequence(packet, _sequenceCounter++);
                _packetQueue.Enqueue(packetWithSequence);
            }
        }

        // 从缓存池获取分包
        public async Task<PacketWithSequence?> GetPacketAsync()
        {
            while (true)
            {
                int queueLength;
                lock (_packetQueue)
                {
                    queueLength = _packetQueue.Count;
                }

                if (queueLength > 0)
                {
                    lock (_packetQueue)
                    {
                        if (_packetQueue.Count > 0)
                        {
                            var packetWithSequence = _packetQueue.Dequeue();
                            _semaphore.Release();
                            return packetWithSequence;
                        }
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_packetQueue)
                {
                    return _packetQueue.Count;
                }
            }
        }
    }
}