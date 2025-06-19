using XiaoZhiSharp_MauiApp.Services;

namespace XiaoZhiSharp_MauiApp
{
    public partial class MainPage : ContentPage
    {
        private readonly XiaoZhi_AgentService _agnetService;
        public MainPage(XiaoZhi_AgentService agnetService)
        {
            InitializeComponent();
            _agnetService = agnetService;
            BindingContext = _agnetService;
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            //按下
            _ = Task.Run(async () =>
            {
                await _agnetService.Agent.StartRecording();
            });
        }

        private void ImageButton_Released(object sender, EventArgs e)
        {
            //松开
            _ = Task.Run(async () =>
            {
                await _agnetService.Agent.StopRecording();
            });

            //_ = Task.Run(async () =>
            //{
            //    await _agnetService.Agent.ChatMessage("你好");
            //});
        }
    }
}
