using MauiSensorKit.SampleApp.ViewModels;

namespace MauiSensorKit.SampleApp.Views;

public partial class BatteryGraphPage : ContentPage
{
    public BatteryGraphPage(BatteryGraphViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
