using MauiSensorKit.SampleApp.Views;

namespace MauiSensorKit.SampleApp;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute("dashboard", typeof(DashboardPage));
        Routing.RegisterRoute("sensors", typeof(SensorSelectionPage));
        Routing.RegisterRoute("activity", typeof(ActivityRecognitionPage));
        Routing.RegisterRoute("route", typeof(RouteTrackerPage));
        Routing.RegisterRoute("battery", typeof(BatteryGraphPage));
        Routing.RegisterRoute("motion", typeof(MotionSensorsPage));
    }
}
