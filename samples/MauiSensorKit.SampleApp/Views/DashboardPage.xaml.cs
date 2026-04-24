using MauiSensorKit.SampleApp.ViewModels;

namespace MauiSensorKit.SampleApp.Views;

public partial class DashboardPage : ContentPage, IDisposable
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnConfigureClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///sensorselection");
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Dispose();
    }
}

/// <summary>
/// Converter to change button text based on recording state.
/// </summary>
public class BoolToRecordButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isRecording)
        {
            return isRecording ? "Recording..." : "Start Recording";
        }
        return "Start Recording";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
