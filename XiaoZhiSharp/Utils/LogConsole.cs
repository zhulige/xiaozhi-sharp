using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XiaoZhiSharp.Utils
{
    // 消息类型枚举
    public enum MessageType
    {
        Send,
        Recv,
        Info,
        Warn,
        Erro
    }

    // 日志控制台类
    public class LogConsole
    {
        public static bool IsWrite {get;set;} = true;
        // 记录消息并换行的方法
        public static void WriteLine(MessageType type, string message)
        {
            WriteMessage(type, message, true);
        }

        public static void WriteLine(string message)
        {
            WriteMessage(MessageType.Info, message, true);
        }

        // 记录消息但不换行的方法
        public static void Write(MessageType type, string message)
        {
            WriteMessage(type, message, false);
        }
        public static void Write(string message)
        {
            WriteMessage(MessageType.Info, message, false);
        }

        // 私有方法，用于处理消息的输出，封装公共逻辑
        private static void WriteMessage(MessageType type, string message, bool isNewLine)
        {
            if (!Global.IsDebug)
                return;

            if (!IsWrite)
                return;

            ConsoleColor originalColor = Console.ForegroundColor;

            try
            {
                // 根据消息类型设置控制台前景色
                SetConsoleColor(type);

                // 格式化消息
                string formattedMessage = FormatMessage(type, Regex.Unescape(message));

                // 根据是否换行选择输出方式
                if (isNewLine)
                {
                    Console.WriteLine(formattedMessage);
                }
                else
                {
                    Console.Write(formattedMessage);
                }
            }
            finally
            {
                // 恢复控制台原始颜色
                Console.ForegroundColor = originalColor;
            }
        }

        // 私有方法，根据消息类型设置控制台前景色
        private static void SetConsoleColor(MessageType type)
        {
            switch (type)
            {
                case MessageType.Send:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case MessageType.Recv:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case MessageType.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case MessageType.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageType.Erro:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    // 若消息类型未匹配，使用默认的灰色
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }

        // 私有方法，格式化消息，添加时间戳和消息类型
        private static string FormatMessage(MessageType type, string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffff}] [{type}] {message}";
        }

        // 快捷方法：发送消息并换行
        public static void SendLine(string message)
        {
            WriteLine(MessageType.Send, message);
        }

        // 快捷方法：发送消息不换行
        public static void Send(string message)
        {
            Write(MessageType.Send, message);
        }

        // 快捷方法：接收消息并换行
        public static void ReceiveLine(string message)
        {
            WriteLine(MessageType.Recv, message);
        }

        // 快捷方法：接收消息不换行
        public static void Receive(string message)
        {
            Write(MessageType.Recv, message);
        }

        // 快捷方法：记录信息并换行
        public static void InfoLine(string message)
        {
            WriteLine(MessageType.Info, message);
        }

        // 快捷方法：记录信息不换行
        public static void Info(string message)
        {
            Write(MessageType.Info, message);
        }

        // 快捷方法：记录警告并换行
        public static void WarningLine(string message)
        {
            WriteLine(MessageType.Warn, message);
        }

        // 快捷方法：记录警告不换行
        public static void Warning(string message)
        {
            Write(MessageType.Warn, message);
        }

        // 快捷方法：记录错误并换行
        public static void ErrorLine(string message)
        {
            WriteLine(MessageType.Erro, message);
        }

        // 快捷方法：记录错误不换行
        public static void Error(string message)
        {
            Write(MessageType.Erro, message);
        }
    }
}
