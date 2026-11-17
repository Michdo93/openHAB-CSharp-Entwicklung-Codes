using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OhDash.ViewModels;
using OhDash.Views;

namespace OhDash;

public partial class App : Application
{
    /// <summary>Das ViewModel, das Program.cs vor dem Start vorbereitet.</summary>
    public static DashboardViewModel? ViewModel { get; set; }
    public static string Gruppe { get; set; } = "";

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new HauptFenster
            {
                DataContext = ViewModel,
                Title = $"ohdash – {Gruppe}",
            };
        base.OnFrameworkInitializationCompleted();
    }
}
