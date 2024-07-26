using LibBundle3.Nodes;
using LibBundledGGPK3;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace PoeFixer;

public class GGPKPatcher
{
    public string? GGPKPath { get; set; }
    public string CachePath { get; set; }
    public string ModifiedFilesPath { get; set; }

    public const string extractJsonPath = "paths_to_extract.json";

    public string[] pathsToSkip = null!;

    public MainWindow window;

    public bool EnableZoom => window.ZoomBox.IsChecked == true;
    public bool EnableMapReveal => window.RevealBox.IsChecked == true;
    public bool RemoveShadows => window.ShadowBox.IsChecked == true;
    public bool RemoveFog => window.FogBox.IsChecked == true;
    public bool EnableColorMods => window.ColorModBox.IsChecked == true;
    public bool RemoveSoftParticles => window.ParticleBox.IsChecked == true;
    public bool RemoveCorpses => window.CorpseBox.IsChecked == true;
    public bool RemoveShatter => window.ShatterBox.IsChecked == true;
    public bool RemoveBloom => window.BloomBox.IsChecked == true;

    public float GetZoomLevel()
    {
        return (float)window.ZoomSlider.Value;
    }

    public void LoadPathsToSkip()
    {
        string[] lines = File.ReadAllLines("paths_to_skip.txt");

        List<string> skipList = [];

        foreach (string line in lines)
        {
            // If the line is empty, or starts with '#', skip it.
            if (line.Length == 0 || line[0] == '#') continue;

            skipList.Add(line);
        }

        pathsToSkip = [.. skipList];
    }

    public void RemoveSkippedAssets()
    {
        foreach (string path in pathsToSkip)
        {
            if (File.Exists($"{ModifiedFilesPath}{path}"))
            {
                File.Delete($"{ModifiedFilesPath}{path}");
            }
            else if (Directory.Exists($"{ModifiedFilesPath}{path}"))
            {
                Directory.Delete($"{ModifiedFilesPath}{path}", true);
            }
        }
    }

    /// <summary>
    /// Moves all patched assets into the GGPK.
    /// I shouldn't use FileRecord.Write but I don't understand how the other one is supposed to work.
    /// </summary>
    public void PatchGGPKWithModifiedAssets(BundledGGPK ggpk, string assetPath)
    {
        // If a zip file exists called "assets.zip", delete it.
        if (File.Exists("assets.zip")) File.Delete("assets.zip");

        // Zip contents of assetPath into assets.zip.
        ZipFile.CreateFromDirectory(assetPath, "assets.zip");

        ZipArchive archive = ZipFile.OpenRead("assets.zip");
        int patchedAssets = LibBundle3.Index.Replace(ggpk.Index, archive.Entries);
        archive.Dispose();

        File.Delete("assets.zip");

        window.EmitToConsole($"Patched {patchedAssets} assets.");
    }

    public GGPKPatcher(MainWindow window)
    {
        this.window = window;

        CachePath = $"{AppDomain.CurrentDomain.BaseDirectory}cache/";
        ModifiedFilesPath = $"{AppDomain.CurrentDomain.BaseDirectory}modifiedfiles/";
    }

    /// <summary>
    /// Returns if GGPK was selected.
    /// </summary>
    /// <returns></returns>
    public bool GetGGPKPath()
    {
        // Open up a file explorer window to select the GGPK file.
        OpenFileDialog ofd = new()
        {
            Filter = "GGPK Files (*.ggpk)|*.ggpk",
            FilterIndex = 1,
            Multiselect = false
        };

        // If a file is selected.
        if (ofd.ShowDialog() == true)
        {
            GGPKPath = ofd.FileName;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extract all vanilla paths in paths_to_extract.json using newtonsoft.
    /// </summary>
    public int ExtractVanillaAssets()
    {
        // Check if ggpk exists at path.
        if (GGPKPath == null)
        {
            window.EmitToConsole("Select the GGPK path first.");
            return 0;
        }

        // Delete cache directory if it exists.
        if (Directory.Exists(CachePath)) Directory.Delete(CachePath, true);

        using BundledGGPK ggpk = new(GGPKPath);

        PathData pathData = JsonConvert.DeserializeObject<PathData>(File.ReadAllText(extractJsonPath))!;

        int count = 0;

        foreach (string path in pathData.paths)
        {
            ggpk.Index.TryFindNode(path, out ITreeNode? node);
            if (node == null) continue;

            string directory = Path.GetDirectoryName(path)!;

            count += LibBundle3.Index.ExtractParallel(node, $"{CachePath}{directory}");
        }

        return count;
    }

    /// <summary>
    /// Return if successfully patched.
    /// </summary>
    public bool Patch()
    {
        LoadPathsToSkip();

        if (!Directory.Exists(CachePath))
        {
            window.EmitToConsole("No assets extracted. Extract vanilla assets first.");
            return false;
        }

        // Clear modified files before patching.
        if (Directory.Exists(ModifiedFilesPath)) Directory.Delete(ModifiedFilesPath, true);

        // Ensure directories exist.
        Directory.CreateDirectory(CachePath);
        Directory.CreateDirectory(ModifiedFilesPath);

        // Check if ggpk exists at path.
        if (GGPKPath == null)
        {
            window.EmitToConsole("Select the GGPK path first.");
            return false;
        }

        BundledGGPK ggpk = new(GGPKPath);

        PatchAssets();

        RemoveSkippedAssets();

        PatchGGPKWithModifiedAssets(ggpk, ModifiedFilesPath);
        if (window.MTXBox.IsChecked == true) ReplaceSkills(ggpk);

        ggpk.Dispose();
        GC.Collect();

        return true;
    }

    private void PatchAssets()
    {
        Stopwatch sw = new();

        // Get every method with Patch attribute.
        MethodInfo[] methods = GetType().GetMethods();
        foreach (MethodInfo method in methods)
        {
            PatchAttribute? patchAttribute = method.GetCustomAttribute<PatchAttribute>();
            if (patchAttribute == null) continue;

            // Make edit text delegate from method.
            string? LocalDelegate(string text, string relativePath)
            {
                return (string?)method.Invoke(this, [text, relativePath]);
            }

            sw.Start();
            EditFilesInPath(patchAttribute.FilePath, LocalDelegate);
            sw.Stop();
            window.EmitToConsole($"Spent {(int)sw.Elapsed.TotalMilliseconds}ms patching {method.Name}");
            sw.Reset();
        }
    }

    public delegate string? EditTextDelegate(string text, string relativePath);

    /// <summary>
    /// Edits files relative to the vanilla cache path and writes them to the modified files path.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="editDelegate"></param>
    public void EditFilesInPath(string path, EditTextDelegate editDelegate)
    {
        string[] files;

        // Check if path has a file extension.
        if (Path.HasExtension(path))
        {
            files = [$"{CachePath}{path}"];
        }
        else
        {
            files = Directory.GetFiles($"{CachePath}{path}", "*", SearchOption.AllDirectories);
        }

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);

            // Get path relative to cache path.
            string relativePath = file.Replace($"{CachePath}", "");

            string? newText = editDelegate(text, relativePath);

            // No modification.
            if (newText == null) continue;

            // Ensure directory exists.
            Directory.CreateDirectory(Path.GetDirectoryName($"{ModifiedFilesPath}{relativePath}")!);

            // Check if file extension is .hlsl.
            if (Path.GetExtension(file).Equals(".hlsl"))
            {
                File.WriteAllText($"{ModifiedFilesPath}{relativePath}", newText, Encoding.ASCII);
            }
            else
            {
                File.WriteAllText($"{ModifiedFilesPath}{relativePath}", newText, Encoding.Unicode);
            }
        }
    }

    [Patch("metadata/characters/character.ot")]
    public string? HandleZoom(string text, string path)
    {
        if (!EnableZoom) return null;
        float zoomLevel = GetZoomLevel();
        return text.Replace("team = 1", $"team = 1\r\n\ton_initial_position_set = \"CreateCameraZoomNode(1000000, 1000000, {zoomLevel});\"");
    }

    [Patch("shaders/minimap_visibility_pixel.hlsl")]
    public string? HandleMapReveal(string text, string path)
    {
        if (!EnableMapReveal) return null;
        return text.Replace("float4(0.0f, 0.0f, 0.0f, 1.0f)", "float4(0.2f, 0.0f, 0.0f, 1.0f)");
    }

    [Patch("metadata/environmentsettings")]
    public string? HandleEnvFiles(string text, string path)
    {
        // If file extension is not .env return null.
        if (!Path.GetExtension(path).Equals(".env")) return null;

        bool patched = false;

        if (RemoveShadows)
        {
            text = text.Replace("\"shadows_enabled\": true", "\"shadows_enabled\": false");
            patched = true;
        }

        if (RemoveFog)
        {
            text = text.Replace("area", "xrea");
            text = text.Replace("fog", "xog");
            patched = true;
        }

        if (!patched) return null;

        return text;
    }

    [Patch("metadata/monsters/monster.ot")]
    public string? HandleCorpses(string text, string path)
    {
        if (!RemoveCorpses) return null;

        text = text.Replace("Life\r\n{\r\n}", "Life\r\n{\r\n\ton_spawned_dead = \"RemoveEffects(); DisableRendering();\"\r\n\ton_death = \"RemoveEffects(); DisableRendering();\"\r\n}");

        string originalText = "slow_animations_go_to_idle = true\r\n}";
        string toReplace = "slow_animations_go_to_idle = true\r\n\ton_start_Revive = \"RemoveEffects(); EnableRendering();\"\r\n}";

        return text.Replace(originalText, toReplace);
    }

    [Patch("shaders/bloomgather.hlsl")]
    public string? HandleBloom(string text, string path)
    {
        if (!RemoveBloom) return null;

        string searchString = "float4 BloomGather";
        int index = text.IndexOf(searchString);
        string newContent = text.Substring(0, index + searchString.Length);

        return newContent + "( PInput input ) : PIXEL_RETURN_SEMANTIC\r\n{\r\n\treturn 0;\r\n}\r\n";
    }

    [Patch("metadata/effects/spells/shatter")]
    public string? HandleShatter(string text, string path)
    {
        if (!RemoveShatter) return null;
        if (!Path.GetExtension(path).Equals(".otc")) return null;

        return text.Replace("Render\r\n{\r\n}", "Render\r\n{\r\n\tdisable_rendering = true\r\n}");
    }

    /// <summary>
    /// Replaces mtx from file.
    /// Will move one directory to another, only replacing files that exist in both directories.
    /// </summary>
    public void ReplaceSkills(BundledGGPK ggpk)
    {
        MTXReplacement[] replacements = JsonConvert.DeserializeObject<MTXReplacement[]>(File.ReadAllText("mtx_replacements.json"))!;

        foreach (MTXReplacement replacement in replacements)
        {
            if (!replacement.enabled) continue;

            if (!ggpk.Index.TryFindNode(replacement.skill, out ITreeNode? node)) continue;

            string mtxDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}freemtx";
            if (Directory.Exists(mtxDirectory)) Directory.Delete(mtxDirectory, true);

            string originalSkillDirectory = $"{CachePath}{replacement.skill}";
            string skillToCopyDirectory = $"{CachePath}{replacement.replacement}";

            string newDirectory = $"{mtxDirectory}/{replacement.skill}";

            // Copy all files from directory to new directory.
            Directory.CreateDirectory(newDirectory);
            foreach (string file in Directory.GetFiles(skillToCopyDirectory))
            {
                // Skip if file doesn't exist in the original directory.
                string relativePath = file.Replace(skillToCopyDirectory, originalSkillDirectory);
                if (!File.Exists(relativePath)) continue;

                File.Copy(file, $"{newDirectory}/{Path.GetFileName(file)}");
            }

            // Zip contents of new directory.
            if (File.Exists("mtx.zip")) File.Delete("mtx.zip");
            ZipFile.CreateFromDirectory(mtxDirectory, "mtx.zip");

            ZipArchive archive = ZipFile.OpenRead("mtx.zip");

            int replaced = LibBundle3.Index.Replace(ggpk.Index, archive.Entries);

            window.EmitToConsole($"Replaced {replaced} files for {replacement.skill}.");

            archive.Dispose();
        }

        if (File.Exists("mtx.zip")) File.Delete("mtx.zip");
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class PatchAttribute : Attribute
{
    public string FilePath { get; }

    public PatchAttribute(string filePath)
    {
        FilePath = filePath;
    }
}