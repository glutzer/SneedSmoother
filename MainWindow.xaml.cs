using LibBundledGGPK3;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace PoeFixer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public GGPKPatcher patcher;

    public MainWindow()
    {
        patcher = new GGPKPatcher(this);

        InitializeComponent();
    }

    private void RestoreExtractedAssets(object sender, RoutedEventArgs e)
    {
        if (patcher.GGPKPath == null)
        {
            EmitToConsole("GGPK is not selected.");
            return;
        }

        // Check if patcher.CachePath exists.
        if (!Directory.Exists(patcher.CachePath))
        {
            EmitToConsole("Cache path does not exist, assets not extracted.");
            return;
        }

        using BundledGGPK ggpk = new(patcher.GGPKPath);

        patcher.PatchGGPKWithModifiedAssets(ggpk, patcher.CachePath);
    }

    private void ExtractVanillaAssets(object sender, RoutedEventArgs e)
    {
        int count = patcher.ExtractVanillaAssets();
        if (count > 0)
        {
            EmitToConsole($"{count} assets extracted from GGPK.");
        }
    }

    private void SelectGGPK(object sender, RoutedEventArgs e)
    {
        if (patcher.GetGGPKPath())
        {
            EmitToConsole($"GGPK selected at {patcher.GGPKPath}.");
        }
    }

    private void PatchGGPK(object sender, RoutedEventArgs e)
    {
        EmitToConsole("Patching GGPK...");
        Stopwatch sw = new();
        sw.Start();
        bool patched = patcher.Patch();
        sw.Stop();
        if (patched) EmitToConsole($"GGPK patched in {(int)sw.Elapsed.TotalMilliseconds}ms.");
    }

    public void EmitToConsole(string line)
    {
        Console.Text += line + "\n";
        Console.ScrollToEnd();
    }
}