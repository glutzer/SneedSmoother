using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Windows.Controls;

namespace PoeFixer;

public class PatchManager
{
    public HashSet<string> patchedFiles = [];

    // Index ambiguous between steam and normal client.
    public LibBundle3.Index index;

    // Paths ending with "/".
    public string CachePath { get; set; }
    public string ModifiedCachePath { get; set; }

    public Dictionary<string, bool> bools = [];
    public Dictionary<string, float> floats = [];

    public MainWindow window;

    public PatchManager(LibBundle3.Index index, MainWindow window)
    {
        CachePath = $"{AppDomain.CurrentDomain.BaseDirectory}extractedassets/";
        ModifiedCachePath = $"{AppDomain.CurrentDomain.BaseDirectory}modifiedassets/";

        this.index = index;
        this.window = window;
    }

    public int RestoreExtractedAssets()
    {
        if (File.Exists("patch.zip")) File.Delete("patch.zip");
        ZipFile.CreateFromDirectory(CachePath, "patch.zip");
        ZipArchive archive = ZipFile.OpenRead("patch.zip");
        int count = LibBundle3.Index.Replace(index, archive.Entries);
        archive.Dispose();
        File.Delete("patch.zip");
        return count;
    }

    public void CollectSettings()
    {
        // Find every named checkbox in the window.
        foreach (Control control in window.mainGrid.Children)
        {
            if (control is CheckBox checkbox)
            {
                bools.Add(checkbox.Name, checkbox.IsChecked == true);
            }

            if (control is Slider slider)
            {
                floats.Add(slider.Name, (float)slider.Value);
            }
        }
    }

    /// <summary>
    /// Main patch method.
    /// </summary>
    public int Patch()
    {
        // Get every class implementing the IPatch interface.
        Type[] patchTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IPatch))).ToArray();

        CollectSettings();

        // Instantiate every patch, only keep enabled ones.
        IPatch[] patches = new IPatch[patchTypes.Length];
        for (int i = 0; i < patchTypes.Length; i++)
        {
            patches[i] = (IPatch)Activator.CreateInstance(patchTypes[i])!;
        }
        patches = patches.Where(x => x.ShouldPatch(bools, floats)).ToArray();

        // Delete modified file directory.
        if (Directory.Exists(ModifiedCachePath))
        {
            Directory.Delete(ModifiedCachePath, true);
        }
        Directory.CreateDirectory(ModifiedCachePath);

        // Modify files.
        foreach (IPatch patch in patches)
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();

            foreach (string file in patch.FilesToPatch)
            {
                TryModifyFile(file, patch);
            }

            foreach (string directory in patch.DirectoriesToPatch)
            {
                string[] extensions = patch.Extension.Split('|');

                for (int i = 0; i < extensions.Length; i++)
                {
                    ModifyDirectory(directory, extensions[i], patch);
                }
            }

            stopWatch.Stop();
            window.EmitToConsole($"{patch.GetType().Name} patched in {(int)stopWatch.Elapsed.TotalMilliseconds}ms.");
        }

        if (File.Exists("patch.zip")) File.Delete("patch.zip");
        ZipFile.CreateFromDirectory(ModifiedCachePath, "patch.zip");
        ZipArchive archive = ZipFile.OpenRead("patch.zip");
        int count = LibBundle3.Index.Replace(index, archive.Entries);
        archive.Dispose();
        //File.Delete("patch.zip");
        return count;
    }

    public void ModifyDirectory(string path, string extension, IPatch patch)
    {
        IEnumerable<string> files = Directory.EnumerateFiles($"{CachePath}{path}", extension, SearchOption.AllDirectories);

        foreach (string file in files)
        {
            ModifyFile(file, patch);
        }
    }

    public void TryModifyFile(string path, IPatch patch)
    {
        path = $"{CachePath}{path}";
        if (File.Exists(path))
        {
            ModifyFile(path, patch);
        }
    }

    public void ModifyFile(string path, IPatch patch)
    {
        bool patchModifiedAsset = patchedFiles.Contains(path);

        // Grab this file from the modified cache if it was modified already.
        if (patchModifiedAsset) path = path.Replace(CachePath, ModifiedCachePath);

        string text = File.ReadAllText(path);

        string? modifiedText = patch.PatchFile(text);
        if (modifiedText == null) return;

        // Write to the modifie d cache.
        if (patchModifiedAsset)
        {
            // Check if extension is glsl.
            if (Path.GetExtension(path) == ".hlsl")
            {
                File.WriteAllText(path, modifiedText, Encoding.ASCII);
            }
            else
            {
                File.WriteAllText(path, modifiedText, Encoding.Unicode);
            }
        }
        else
        {
            string modifiedPath = path.Replace(CachePath, ModifiedCachePath);

            // Ensure path exists.
            Directory.CreateDirectory(Path.GetDirectoryName(modifiedPath)!);

            if (Path.GetExtension(modifiedPath) == ".hlsl")
            {
                File.WriteAllText(modifiedPath, modifiedText, Encoding.ASCII);
            }
            else
            {
                File.WriteAllText(modifiedPath, modifiedText, Encoding.Unicode);
            }

            patchedFiles.Add(path);
        }
    }
}