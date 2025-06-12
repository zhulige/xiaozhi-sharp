using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace XiaoZhiSharp.Protocols
{
    /// <summary>
    /// OTA请求模型
    /// </summary>
    public class OtaRequest
    {
        [JsonProperty("application")]
        public ApplicationInfo Application { get; set; } = new ApplicationInfo();

        [JsonProperty("mac_address", NullValueHandling = NullValueHandling.Ignore)]
        public string? MacAddress { get; set; }

        [JsonProperty("uuid", NullValueHandling = NullValueHandling.Ignore)]
        public string? Uuid { get; set; }

        [JsonProperty("chip_model_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? ChipModelName { get; set; }

        [JsonProperty("flash_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? FlashSize { get; set; }

        [JsonProperty("psram_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? PsramSize { get; set; }

        [JsonProperty("partition_table", NullValueHandling = NullValueHandling.Ignore)]
        public List<PartitionInfo>? PartitionTable { get; set; }

        [JsonProperty("board")]
        public BoardInfo Board { get; set; } = new BoardInfo();

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public int? Version { get; set; }

        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string? Language { get; set; }

        [JsonProperty("minimum_free_heap_size", NullValueHandling = NullValueHandling.Ignore)]
        public long? MinimumFreeHeapSize { get; set; }

        [JsonProperty("ota", NullValueHandling = NullValueHandling.Ignore)]
        public OtaInfo? Ota { get; set; }
    }

    /// <summary>
    /// 应用程序信息
    /// </summary>
    public class ApplicationInfo
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; } = "xiaozhi";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonProperty("elf_sha256")]
        public string ElfSha256 { get; set; } = "";

        [JsonProperty("compile_time", NullValueHandling = NullValueHandling.Ignore)]
        public string? CompileTime { get; set; }

        [JsonProperty("idf_version", NullValueHandling = NullValueHandling.Ignore)]
        public string? IdfVersion { get; set; }
    }

    /// <summary>
    /// 分区信息
    /// </summary>
    public class PartitionInfo
    {
        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("subtype")]
        public int Subtype { get; set; }

        [JsonProperty("address")]
        public long Address { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// 开发板信息
    /// </summary>
    public class BoardInfo
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("ssid", NullValueHandling = NullValueHandling.Ignore)]
        public string? Ssid { get; set; }

        [JsonProperty("rssi", NullValueHandling = NullValueHandling.Ignore)]
        public int? Rssi { get; set; }

        [JsonProperty("channel", NullValueHandling = NullValueHandling.Ignore)]
        public int? Channel { get; set; }

        [JsonProperty("ip", NullValueHandling = NullValueHandling.Ignore)]
        public string? Ip { get; set; }

        [JsonProperty("mac", NullValueHandling = NullValueHandling.Ignore)]
        public string? Mac { get; set; }

        [JsonProperty("revision", NullValueHandling = NullValueHandling.Ignore)]
        public string? Revision { get; set; }

        [JsonProperty("carrier", NullValueHandling = NullValueHandling.Ignore)]
        public string? Carrier { get; set; }

        [JsonProperty("csq", NullValueHandling = NullValueHandling.Ignore)]
        public string? Csq { get; set; }

        [JsonProperty("imei", NullValueHandling = NullValueHandling.Ignore)]
        public string? Imei { get; set; }

        [JsonProperty("iccid", NullValueHandling = NullValueHandling.Ignore)]
        public string? Iccid { get; set; }
    }

    /// <summary>
    /// OTA信息
    /// </summary>
    public class OtaInfo
    {
        [JsonProperty("label")]
        public string Label { get; set; } = "";
    }

    /// <summary>
    /// OTA响应模型
    /// </summary>
    public class OtaResponse
    {
        [JsonProperty("activation", NullValueHandling = NullValueHandling.Ignore)]
        public ActivationInfo? Activation { get; set; }

        [JsonProperty("mqtt", NullValueHandling = NullValueHandling.Ignore)]
        public MqttInfo? Mqtt { get; set; }

        [JsonProperty("websocket", NullValueHandling = NullValueHandling.Ignore)]
        public WebSocketInfo? WebSocket { get; set; }

        [JsonProperty("server_time", NullValueHandling = NullValueHandling.Ignore)]
        public ServerTimeInfo? ServerTime { get; set; }

        [JsonProperty("firmware", NullValueHandling = NullValueHandling.Ignore)]
        public FirmwareInfo? Firmware { get; set; }
    }

    /// <summary>
    /// 激活信息
    /// </summary>
    public class ActivationInfo
    {
        [JsonProperty("code")]
        public string Code { get; set; } = "";

        [JsonProperty("message")]
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// MQTT配置信息
    /// </summary>
    public class MqttInfo
    {
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; } = "";

        [JsonProperty("client_id")]
        public string ClientId { get; set; } = "";

        [JsonProperty("username")]
        public string Username { get; set; } = "";

        [JsonProperty("password")]
        public string Password { get; set; } = "";

        [JsonProperty("publish_topic")]
        public string PublishTopic { get; set; } = "";
    }

    /// <summary>
    /// WebSocket配置信息
    /// </summary>
    public class WebSocketInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; } = "";

        [JsonProperty("token")]
        public string Token { get; set; } = "";
    }

    /// <summary>
    /// 服务器时间信息
    /// </summary>
    public class ServerTimeInfo
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; } = "";

        [JsonProperty("timezone_offset")]
        public int TimezoneOffset { get; set; }
    }

    /// <summary>
    /// 固件信息
    /// </summary>
    public class FirmwareInfo
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "";

        [JsonProperty("url")]
        public string Url { get; set; } = "";
    }

    /// <summary>
    /// OTA错误响应
    /// </summary>
    public class OtaErrorResponse
    {
        [JsonProperty("error")]
        public string Error { get; set; } = "";
    }
} 