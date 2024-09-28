﻿using LibBundledGGPK3;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace PoeFixer;

public partial class MainWindow : Window
{
    public string? GGPKPath { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    public void EmitToConsole(string line)
    {
        Console.Text += line + "\n";
        Console.ScrollToEnd();
    }

    private void RestoreExtractedAssets(object sender, RoutedEventArgs e)
    {
        if (GGPKPath == null)
        {
            EmitToConsole("GGPK is not selected.");
            return;
        }

        // Check if ggpk extension is .ggpk.
        if (GGPKPath.EndsWith(".ggpk"))
        {
            BundledGGPK ggpk = new(GGPKPath);
            PatchManager manager = new(ggpk.Index, this);
            int count = manager.RestoreExtractedAssets();
            ggpk.Dispose();
            EmitToConsole($"{count} assets restored.");
        }

        if (GGPKPath.EndsWith(".bin"))
        {
            LibBundle3.Index index = new(GGPKPath);
            PatchManager manager = new(index, this);
            int count = manager.RestoreExtractedAssets();
            index.Dispose();
            EmitToConsole($"{count} assets restored.");
        }
    }

    private void ExtractVanillaAssets(object sender, RoutedEventArgs e)
    {
        if (GGPKPath == null)
        {
            EmitToConsole("GGPK is not selected.");
            return;
        }

        if (GGPKPath.EndsWith(".ggpk"))
        {
            BundledGGPK ggpk = new(GGPKPath);
            FileExtractor extractor = new(ggpk.Index);
            int count = extractor.ExtractFiles();
            ggpk.Dispose();
            EmitToConsole($"{count} assets extracted.");
        }

        if (GGPKPath.EndsWith(".bin"))
        {
            LibBundle3.Index index = new(GGPKPath);
            FileExtractor extractor = new(index);
            int count = extractor.ExtractFiles();
            index.Dispose();
            EmitToConsole($"{count} assets extracted.");
        }
    }

    private void SelectGGPK(object sender, RoutedEventArgs e)
    {
        // Open file dialogue to select either a .ggpk or .bin file.
        OpenFileDialog dlg = new()
        {
            DefaultExt = ".ggpk",
            Filter = "GGPK Files (*.ggpk, *.bin)|*.ggpk;*.bin"
        };

        if (dlg.ShowDialog() == true)
        {
            GGPKPath = dlg.FileName;
            EmitToConsole($"GGPK selected: {GGPKPath}.");
        }
    }

    private void PatchGGPK(object sender, RoutedEventArgs e)
    {
        if (GGPKPath == null)
        {
            EmitToConsole("GGPK is not selected.");
            return;
        }

        EmitToConsole("Patching GGPK...");
        Stopwatch sw = new();
        sw.Start();

        if (GGPKPath.EndsWith(".ggpk"))
        {
            BundledGGPK ggpk = new(GGPKPath);
            PatchManager manager = new(ggpk.Index, this);
            int count = manager.Patch();
            ggpk.Dispose();
            EmitToConsole($"{count} assets patched.");
        }

        if (GGPKPath.EndsWith(".bin"))
        {
            LibBundle3.Index index = new(GGPKPath);
            PatchManager manager = new(index, this);
            int count = manager.Patch();
            index.Dispose();
            EmitToConsole($"{count} assets patched.");
        }

        sw.Stop();
        EmitToConsole($"GGPK patched in {(int)sw.Elapsed.TotalMilliseconds}ms.");
    }
}