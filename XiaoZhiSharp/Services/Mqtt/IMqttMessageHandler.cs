using System.Threading.Tasks;

namespace XiaozhiAI.Services.Mqtt
{
    /// <summary>
    /// MQTT消息处理器接口
    /// </summary>
    public interface IMqttMessageHandler
    {
        /// <summary>
        /// 处理接收到的MQTT消息
        /// </summary>
        /// <param name="topic">消息主题</param>
        /// <param name="payload">消息内容</param>
        /// <returns>处理任务</returns>
        Task HandleMessageAsync(string topic, string payload);
        
        /// <summary>
        /// 检查此处理器是否可以处理指定主题的消息
        /// </summary>
        /// <param name="topic">消息主题</param>
        /// <returns>如果可以处理返回true，否则返回false</returns>
        bool CanHandle(string topic);
    }
}