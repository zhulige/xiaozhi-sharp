using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using System.Collections.Generic;
using XiaoZhiSharp.Services;

namespace AI_;

public partial class Test : Node2D
{
    
    #region web设置参数
    public string OTA_VERSION_URL { get; set; } = "https://api.tenclass.net/xiaozhi/ota/";
    public string WEB_SOCKET_URL { get; set; } = "wss://api.tenclass.net/xiaozhi/v1/";
    public string TOKEN { get; set; } = "test-token";
    public bool IsLogWrite { get { return LogConsole.IsWrite; } set { LogConsole.IsWrite = value; } }
    public bool IsAudio { get; set; } = true;
    public bool IsOTA { get; set; } = true;
    public IoTCommandHandler handler;
    public delegate void MessageEventHandler(string message);
    public delegate void AudioEventHandler(byte[] opus);
    public delegate void IotEventHandler(string message);
    public event MessageEventHandler? OnMessageEvent = null;
    public event AudioEventHandler? OnAudioEvent = null;
    public event IotEventHandler? OnIotEvent = null;


    private WebSocketService? _webSocketService = null;
    private AudioService? _audioService = null;
    private Thread? _sendOpusthread = null;
    #endregion

    #region 麦克风音频参数

    private AudioEffectRecord _effect;
    private AudioStreamWav _recording;
    private bool _isRecording = false;
    private Task _recordingTask;
    [Export] private Button PlayButton;
    [Export] private Button SaveButton;
    [Export] private Button RecordButton;
    [Export] private Label Status;
    [Export] private AudioStreamPlayer AudioStreamRecord;
    [Export] private AudioStreamPlayer AudioStreamPlayer;
    [Export] private RichTextLabel RichTextLabeltext;
    private AudioService opuService;    

    #endregion
    
    public async override void _Ready()
    {
        Start();
        // 1. 注册生成设备描述
        var descriptor = new IoTDescriptor();
        descriptor.AddDevice(new Lamp());
        descriptor.AddDevice(new DuoJI());
        descriptor.AddDevice(new Camre());
        handler = new IoTCommandHandler(new Lamp(), new DuoJI(), new Camre());
        string descriptorJson = JsonConvert.SerializeObject(descriptor, Formatting.Indented);
        await Task.Delay(1000);
        await IotInit(descriptorJson); //初始化设备
        
        PlayButton.Pressed += OnPlayButtonPressed;
        SaveButton.Pressed += OnSaveButtonPressed;
        RecordButton.ButtonDown += OnRecordButtonDown;
        RecordButton.ButtonUp += OnRecordButtonUp;
        int idx = AudioServer.GetBusIndex("Record");
        _effect = (AudioEffectRecord)AudioServer.GetBusEffect(idx, 0);
    }

    private async void OnRecordButtonDown()
    {
        if (_isRecording) return;
        
        GD.Print("开始录音");
        _isRecording = true;
        _effect.SetRecordingActive(true);
        RecordButton.Text = "正在录音...";
        Status.Text = "Recording...";
        await Send_Listen_Start("manual");
        
        // 等待一小段时间让录音系统初始化
        await Task.Delay(100);
        
        // 启动录音数据发送线程
        _recordingTask = Task.Run(async () =>
        {
            DateTime lastSendTime = DateTime.Now;
            while (_isRecording)
            {
                try
                {
                    // 检查录音是否激活
                    if (!_effect.IsRecordingActive())
                    {
                        await Task.Delay(50);
                        continue;
                    }

                    var recording = _effect.GetRecording();
                    if (recording != null && recording.Data != null && recording.Data.Length > 0)
                    {
                        // 使用AudioService处理音频数据
                        opuService.AddRecordSamples(recording.Data, recording.Data.Length);

                        // 发送音频数据
                        byte[]? opusData;
                        bool hasData = false;
                        while (opuService.OpusRecordTryDequeuee(out opusData))
                        {
                            if (opusData != null)
                            {
                                hasData = true;
                                await _webSocketService.SendOpusAsync(opusData);
                                lastSendTime = DateTime.Now;
                            }
                            await Task.Delay(5); // 减少发送间隔
                        }

                        // 如果没有数据发送，但距离上次发送时间超过200ms，发送一个空包保持连接
                        if (!hasData && (DateTime.Now - lastSendTime).TotalMilliseconds > 200)
                        {
                            await _webSocketService.SendOpusAsync(new byte[0]);
                            lastSendTime = DateTime.Now;
                        }
                    }
                    await Task.Delay(20); // 减少检查间隔
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"录音处理错误: {ex.Message}");
                    await Task.Delay(50);
                }
            }
        });
    }

    private async void OnRecordButtonUp()
    {
        if (!_isRecording) return;
        
        GD.Print("停止录音");
        _isRecording = false;
        _effect.SetRecordingActive(false);
        RecordButton.Text = "Record";
        Status.Text = "";
        
        // 等待录音任务完成
        if (_recordingTask != null)
        {
            try
            {
                await _recordingTask;
                
                // 确保最后的数据被发送
                var recording = _effect.GetRecording();
                if (recording != null && recording.Data != null && recording.Data.Length > 0)
                {
                    opuService.AddRecordSamples(recording.Data, recording.Data.Length);
                    byte[]? opusData;
                    while (opuService.OpusRecordTryDequeuee(out opusData))
                    {
                        if (opusData != null)
                        {
                            await _webSocketService.SendOpusAsync(opusData);
                            await Task.Delay(5);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"录音任务结束错误: {ex.Message}");
            }
        }
        
        // 发送停止消息
        await Send_Listen_Stop();
        GD.Print("音频发送完成");
    }

    private async void OnPlayButtonPressed()
    {
        GD.Print("待定功能");
    }
    private void OnSaveButtonPressed()
    {
        GD.Print("OnSaveButtonPressed");
        Send_Listen_Detect("你好啊,当前虚拟设备有啥");//打断
        Status.Text = "打断中..";
    }

    #region web

     public void Start()
     {
         GD.Print("创建web");
         if (IsAudio)
         {
             _audioService = new AudioService(AudioStreamPlayer);
             opuService = _audioService;  // 将_audioService赋值给opuService
             GD.Print("创建_audioService、opuService");
         }
        
         _webSocketService = new WebSocketService(WEB_SOCKET_URL, TOKEN, "5e:87:4d:31:d8:7c");
         GD.Print("创建WebSocketService");
         _webSocketService.OnMessageEvent += WebSocketService_OnMessageEvent;
         _webSocketService.OnAudioEvent += WebSocketService_OnAudioEvent;

         //录音监控，一旦有数据就开始发送
         _sendOpusthread = new Thread(async () =>
         {
             GD.Print("创建__sendOpusthread");
             while (true)
             {
                 if (_audioService == null)
                     return;

                 byte[]? opusData;
                 if (_audioService.OpusRecordTryDequeuee(out opusData))
                 {
                     if (opusData == null)
                     {
                         continue;
                     }
                     await _webSocketService.SendOpusAsync(opusData);
                 }
                 await Task.Delay(60);
             }
         });
         _sendOpusthread.Start();
     }

    public void Stop() {
        _audioService = null;
        _webSocketService = null;
    }

    public void Restart() {
        Stop();
        Start();
    }
    private void WebSocketService_OnAudioEvent(byte[] opus)
    {
        //GD.Print("接收到服务端来的语音消息");
        if (_audioService != null)
            _audioService.OpusPlayEnqueue(opus);

    }

    private void WebSocketService_OnMessageEvent(string message)
    {
        try
        {
            //GD.Print($"接收到服务端消息: {message}");
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
            
            if (jsonData == null) return;

            string type = jsonData["type"].ToString();
            string sessionId = jsonData["session_id"]?.ToString() ?? "";

            switch (type)
            {
                case "stt":
                    // 语音识别结果
                    string text = jsonData["text"]?.ToString() ?? "";
                    LogConsole.ReceiveLine($"用户: {text}");
                    CallDeferred(nameof(SetText),$"用户: {text}");
                    break;

                case "llm":
                    // AI回复
                    string aiText = jsonData["text"]?.ToString() ?? "";
                    string emotion = jsonData["emotion"]?.ToString() ?? "";
                    LogConsole.ReceiveLine($"小智: {aiText}");
                    break;

                case "tts":
                    string state = jsonData["state"]?.ToString() ?? "";
                    switch (state)
                    {
                        case "start":
                            LogConsole.ReceiveLine("小智开始说话...");
                            break;
                        case "sentence_start":
                            string ttsText = jsonData["text"]?.ToString() ?? "";
                            LogConsole.ReceiveLine($"小智: {ttsText}");
                            CallDeferred(nameof(SetText),$"小智: {ttsText}");
                            break;
                        case "sentence_end":
                            LogConsole.ReceiveLine("小智说完一句话");
                            break;
                        case "stop":
                            LogConsole.ReceiveLine("小智停止说话");
                            break;
                    }
                    break;

                case "hello":
                    LogConsole.ReceiveLine("连接已建立");
                    break;
                case "iot":
                    
                    var data= handler.HandleCommand(message);
                    if (data.Success)
                    {
                        Task.Run(async () => await IotState(data.StateJson));                    
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"解析消息时出错: {ex.Message}");
        }
    }

    private void SetText(string msg)
    {
        RichTextLabeltext.Text += msg+"\n";
    }

    #endregion

    #region 协议
    public async Task Send_Hello()
    {
        if(_webSocketService!=null)
            await _webSocketService.SendMessageAsync(WebSocketProtocol.Hello());
    }

    public async Task IotInit(string iotjson)
    {
        if (_webSocketService != null && _audioService != null)
        {
            //Console.WriteLine("生成的设备描述JSON：\n" + iotjson);
            await _webSocketService.SendMessageAsync(iotjson);
        }
    }

    public async Task IotState(string statejson)
    {
        if (_webSocketService != null && _audioService != null)
        {
            await _webSocketService.SendMessageAsync(statejson);
        }
    }

    public async Task Send_Listen_Detect(string text)
    {
        if (_webSocketService != null)
            await _webSocketService.SendMessageAsync(WebSocketProtocol.Listen_Detect(text));
    }
    public async Task Send_Listen_Start(string mode="manual")
    {
        if (_webSocketService != null && _audioService!=null)
        {
            await _webSocketService.SendMessageAsync(WebSocketProtocol.Listen_Start(_webSocketService.SessionId, mode));
            _audioService.StartRecording();
        }
    }
    public async Task Send_Listen_Stop()
    {

        if (_webSocketService != null && _audioService != null)
        {
            await _webSocketService.SendMessageAsync(WebSocketProtocol.Listen_Stop(_webSocketService.SessionId));
            _audioService.StopRecording();
        }
    }

    #endregion
    
}