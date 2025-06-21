using Camera.MAUI;
using DuoDuo.Services;

namespace DuoDuo
{
    public partial class MainPage : ContentPage
    {
        private readonly AgentService _agentService;
        private readonly CameraService _cameraService;

        public MainPage(AgentService agentService,CameraService cameraService)
        {
            InitializeComponent();
            _agentService = agentService;
            _cameraService = cameraService;
            BindingContext = agentService.MainPageModel;

            cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        }

        private void CameraView_CamerasLoaded(object? sender, EventArgs e)
        {
            if (cameraView.NumCamerasDetected > 0)
            {
                if (cameraView.NumMicrophonesDetected > 0)
                    cameraView.Microphone = cameraView.Microphones.First();
                cameraView.Camera = cameraView.Cameras.First();
                //cameraView.Camera = cameraView.Cameras[1];
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (await cameraView.StartCameraAsync() == CameraResult.Success)
                    {
                        //controlButton.Text = "Stop";
                        //playing = true;
                    }
                });
            }
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await _agentService.Agent.StartRecording();
            });
        }

        private void ImageButton_Released(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await _agentService.Agent.StopRecording();
            });
            //_ = Task.Run(async () =>
            //{
            //    await _agentService.Agent.ChatMessage("你好");
            //});
        }

        private void SettingButton_Pressed(object sender, EventArgs e)
        {
            //_ = Task.Run(async () =>
            //{
            //    await _agentService.Agent.StartRecording();
            //});
        }

        private void SettingButton_Released(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                await _agentService.Agent.ChatAbort();
            });
            //_ = Task.Run(async () =>
            //{
            //    await _agentService.Agent.ChatMessage("你好");
            //});
        }

        private void CameraButton_Pressed(object sender, EventArgs e)
        {

        }

        private void CameraButton_Released(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                //byte[]? imageData = await _cameraService.CapturePhotoAsync();
                //if (imageData != null)
                //{
                //    XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
                //    imageStorageService.PostImage("https://coze.nbee.net/image/v1/stream/" + Global.DeviceId, "", Global.DeviceId, Global.ClientId, imageData);
                //    imageStorageService.PostImage(Global.McpVisionUrl, Global.McpVisionToken, Global.DeviceId, Global.ClientId, imageData);
                //}

                var stream = await cameraView.TakePhotoAsync();
                if (stream != null)
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    byte[]? imageData = memoryStream.ToArray();
                    XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
                    imageStorageService.PostImage("https://coze.nbee.net/image/v1/stream/" + Global.DeviceId, "", Global.DeviceId, Global.ClientId, imageData);
                }
            });
        }
    }
}
