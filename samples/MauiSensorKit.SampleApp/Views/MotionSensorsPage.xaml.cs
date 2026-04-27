namespace MauiSensorKit.SampleApp.Views;

public partial class MotionSensorsPage : ContentPage
{
    public MotionSensorsPage(ViewModels.MotionSensorsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Sensors continue running if already started
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Don't stop recording when navigating away - VM is singleton
    }
}
