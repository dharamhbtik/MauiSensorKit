using MauiSensorKit.SampleApp.ViewModels;
using System.ComponentModel;

namespace MauiSensorKit.SampleApp.Views;

public partial class RouteTrackerPage : ContentPage
{
    private RouteTrackerViewModel? _viewModel;

    public RouteTrackerPage(RouteTrackerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateWebViewSource();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Dispose();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RouteTrackerViewModel.MapHtml))
        {
            MainThread.BeginInvokeOnMainThread(UpdateWebViewSource);
        }
    }

    private void UpdateWebViewSource()
    {
        if (_viewModel?.MapHtml is string html && !string.IsNullOrEmpty(html))
        {
            MapWebView.Source = new HtmlWebViewSource { Html = html };
        }
    }
}
