namespace MauiSensorKit.SampleApp.Views;

public partial class ActivityRecognitionPage : ContentPage
{
    public ActivityRecognitionPage(ViewModels.ActivityRecognitionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Page returns to foreground - VM continues running in background
        // UI will automatically update via data bindings
    }
}
