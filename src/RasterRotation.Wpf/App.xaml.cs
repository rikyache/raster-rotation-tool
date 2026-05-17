using System.IO;
using System.Windows;

namespace RasterRotation.Wpf;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var initialFile = e.Args.FirstOrDefault(arg => !arg.StartsWith("-", StringComparison.Ordinal) && File.Exists(arg));
        var window = new MainWindow(initialFile);
        window.Show();
    }
}
