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
                    if (await cameraView.StartCameraAsync(new Size(320,240)) == CameraResult.Success)
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
                var stream = await cameraView.TakePhotoAsync();
                if (stream != null)
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    byte[]? imageData = memoryStream.ToArray();
                    Global.PhotoData = imageData;
                    XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
                    await imageStorageService.PostImage("https://coze.nbee.net/image/v1/stream/" + Global.DeviceId, "", Global.DeviceId, Global.ClientId, imageData);
                }
            });

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
                var stream = await cameraView.TakePhotoAsync(Camera.MAUI.ImageFormat.JPEG);
                if (stream != null)
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    byte[]? imageData = memoryStream.ToArray();
                    Global.PhotoData = imageData;
                    XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
                    await imageStorageService.PostImage("https://coze.nbee.net/image/v1/stream/" + Global.DeviceId, "", Global.DeviceId, Global.ClientId, imageData);
                    await Task.Run(async () =>
                    {
                        await _agentService.Agent.ChatMessage("拍张照片，看看是什么？");
                    });

                    

                }
            });
        }
    }
}
