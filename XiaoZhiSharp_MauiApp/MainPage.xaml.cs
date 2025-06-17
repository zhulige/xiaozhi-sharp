namespace XiaoZhiSharp_MauiApp
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private void ImageButton_Pressed(object sender, EventArgs e)
        {
            CounterBtn.Text = $"按下";
        }

        private void ImageButton_Released(object sender, EventArgs e)
        {
            CounterBtn.Text = $"松开";
        }
    }
}
