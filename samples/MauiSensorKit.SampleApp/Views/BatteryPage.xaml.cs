namespace MauiSensorKit.SampleApp.Views;

using MauiSensorKit.SampleApp.ViewModels;

public partial class BatteryPage : ContentPage
{
    private readonly BatteryViewModel _viewModel;

    public BatteryPage(BatteryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _viewModel.LoadHistoryCommand.ExecuteAsync(null);
    }
}
