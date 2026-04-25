namespace MauiSensorKit.SampleApp.Views;

public partial class ActivityRecognitionPage : ContentPage
{
    public ActivityRecognitionPage(ViewModels.ActivityRecognitionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop monitoring when leaving the page
        if (BindingContext is ViewModels.ActivityRecognitionViewModel vm)
        {
            vm.StopMonitoringCommand.ExecuteAsync(null);
        }
    }
}
