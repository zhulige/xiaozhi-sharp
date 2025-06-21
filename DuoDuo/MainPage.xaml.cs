using DuoDuo.Services;
using System.Threading.Tasks;

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

        private void CameraButton_Pressed(object sender, EventArgs e)
        {

        }

        private void CameraButton_Released(object sender, EventArgs e)
        {
            _ = Task.Run(async () =>
            {
                byte[]? imageData = await _cameraService.CapturePhotoAsync();
                if (imageData != null)
                {
                    XiaoZhiSharp.Services.ImageStorageService imageStorageService = new XiaoZhiSharp.Services.ImageStorageService();
                    imageStorageService.PostImage("https://coze.nbee.net/image/v1/stream/" + Global.DeviceId, "", Global.DeviceId, Global.ClientId, imageData);
                }
            });
        }
    }
}
