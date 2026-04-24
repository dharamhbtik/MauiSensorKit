using MauiSensorKit.SampleApp.ViewModels;

namespace MauiSensorKit.SampleApp.Views;

public partial class SensorSelectionPage : ContentPage
{
    private readonly SensorSelectionViewModel _viewModel;

    public SensorSelectionPage(SensorSelectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAvailabilityCommand.ExecuteAsync(null);
    }
}
