using MauiSensorKit.SampleApp.ViewModels;

namespace MauiSensorKit.SampleApp.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private bool _hasCheckedSensors = false;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Check if any sensors are enabled on first appearance
        if (!_hasCheckedSensors)
        {
            _hasCheckedSensors = true;
            await CheckAndRedirectIfNoSensorsAsync();
        }
    }

    private async Task CheckAndRedirectIfNoSensorsAsync()
    {
        try
        {
            // Check if any sensor is enabled
            var hasEnabledSensors = _viewModel.ConfiguredSensors.Any(s => s.IsEnabled);
            
            if (!hasEnabledSensors)
            {
                // Show alert and redirect to setup
                await DisplayAlert("Setup Required", "No sensors are enabled. Please configure sensors first.", "OK");
                await Shell.Current.GoToAsync("///setup");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking sensors: {ex.Message}");
        }
    }

    private async void OnConfigureClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///setup");
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

/// <summary>
/// Converter that returns true if string is not null or empty.
/// </summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is string str && !string.IsNullOrEmpty(str);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converter that inverts a boolean value.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}
