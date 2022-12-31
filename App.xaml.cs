namespace MauiMvvm;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
    }
    protected override Window CreateWindow(IActivationState activationState)
    {
        Window window = base.CreateWindow(activationState);
        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
        {
            window.Width = 600;
            window.Height = 900;
        }

        if (DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
        {
            window.MinimumHeight = 900;
            window.MaximumHeight = 900;
            window.MinimumWidth = 600;
            window.MaximumWidth = 600;
        }
        return window;
    }
}
