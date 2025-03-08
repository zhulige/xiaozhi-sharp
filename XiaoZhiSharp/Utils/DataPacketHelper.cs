using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    public class DataPacketHelper
    {
        /// <summary>
        /// 移除字节数组中的空字节
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] RemoveEmptyBytes(byte[] input)
        {
            List<byte> nonEmptyBytes = new List<byte>();
            foreach (byte b in input)
            {
                if (b != 0)
                {
                    nonEmptyBytes.Add(b);
                }
            }
            return nonEmptyBytes.ToArray();
        }
    }
}
