using LibBundle3.Nodes; // Required for ITreeNode
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Windows.Controls;

namespace PoeFixer;

public class PatchManager
{
    // No longer need public HashSet<string> patchedFiles;
    // CachePath and ModifiedCachePath are no longer primary ways to handle patch data.
    // We might still need CachePath if RestoreExtractedAssets isn't updated or if initial extraction is still a separate step.
    // For now, let's assume initial extraction to CachePath via FileExtractor is still done if needed by other logic
    // or as a prerequisite if direct GGPK reads are too slow/complex.
    public string CachePath { get; set; }

    public LibBundle3.Index index;
    public Dictionary<string, bool> bools = [];
    public Dictionary<string, float> floats = [];
    public MainWindow window;

    // In-memory store for patched file content (relativePath -> content)
    private Dictionary<string, string> inMemoryPatchedFiles = [];
    // In-memory store for original file content to avoid re-extracting (relativePath -> content)
    private Dictionary<string, string> originalFileCache = [];


    public PatchManager(LibBundle3.Index index, MainWindow window)
    {
        CachePath = $"{AppDomain.CurrentDomain.BaseDirectory}extractedassets/";
        // ModifiedCachePath = $"{AppDomain.CurrentDomain.BaseDirectory}modifiedassets/"; // No longer needed

        this.index = index;
        this.window = window;
    }

    // This method likely needs to change if CachePath isn't reliably populated,
    // or if we want to restore from the GGPK directly.
    // For now, keeping it as is, assuming CachePath contains original extracted files.
    public int RestoreExtractedAssets()
    {
        // This method implies that `CachePath` contains the vanilla files.
        // If we move away from extracting everything to CachePath initially, this method needs a rethink.
        // For now, let's assume FileExtractor.ExtractFiles() has been run.
        if (!Directory.Exists(CachePath) || !Directory.EnumerateFileSystemEntries(CachePath).Any())
        {
            window.EmitToConsole("Error: CachePath is empty or does not exist. Cannot restore assets.");
            window.EmitToConsole("Please ensure vanilla assets are extracted first if you intend to use Restore.");
            // Potentially trigger FileExtractor.ExtractFiles() here if desired, or disable restore button.
            // For now, just returning 0.
            return 0;
        }

        string tempZipPath = Path.Combine(Path.GetTempPath(), "restore_patch.zip");
        if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

        ZipFile.CreateFromDirectory(CachePath, tempZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        int count = 0;
        using (ZipArchive archive = ZipFile.OpenRead(tempZipPath))
        {
            count = LibBundle3.Index.Replace(index, archive.Entries);
        }
        File.Delete(tempZipPath);
        window.EmitToConsole($"Restored {count} original files to GGPK.");
        return count;
    }

    public void CollectSettings()
    {
        bools.Clear(); // Clear previous settings
        floats.Clear();

        foreach (Control control in window.mainGrid.Children)
        {
            if (control is CheckBox checkbox && !string.IsNullOrEmpty(checkbox.Name))
            {
                bools[checkbox.Name] = checkbox.IsChecked == true;
            }

            if (control is Slider slider && !string.IsNullOrEmpty(slider.Name))
            {
                floats[slider.Name] = (float)slider.Value;
            }
        }
    }

    /// <summary>
    /// Extracts a single file from GGPK to a string.
    /// NOTE: This is a placeholder. Actual implementation depends on LibBundle3.Index capabilities.
    /// </summary>
    private string? ExtractSingleFileToString(string relativePath, Encoding encoding)
    {
        if (originalFileCache.TryGetValue(relativePath, out var cachedContent))
        {
            return cachedContent;
        }

        if (index.TryFindNode(relativePath, out ITreeNode? node) && node is FileNode fileNode)
        {
            // Assumption: fileNode.GetDecompressedData() returns byte[] or similar.
            // This part is highly dependent on LibBundle3.Node / LibBundle3.FileNode actual API.
            // For demonstration, let's assume a method GetDecompressedData() exists on FileNode.
            // byte[] fileData = fileNode.GetDecompressedData(); // This is a hypothetical method.

            // Fallback: If direct memory extraction isn't simple,
            // extract to a temporary file and read it. This is less efficient.
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string tempFilePath = Path.Combine(tempDir, Path.GetFileName(relativePath));

            try
            {
                // LibBundle3.Index.ExtractParallel might be too complex for a single file.
                // Let's assume there's a simpler way or we adapt.
                // If LibBundle3.Index.Extract(fileNode, tempDir) or similar exists:
                // LibBundle3.Index.Extract(fileNode, tempDir); // Extracts to tempDir/relativePath
                // string extractedPath = Path.Combine(tempDir, relativePath); // This might be nested

                // Simpler: If FileNode itself can write its content to a stream:
                // using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write)) {
                //    fileNode.WriteToStream(fs); // Hypothetical
                // }
                // string content = File.ReadAllText(tempFilePath, encoding);

                // If we absolutely must use ExtractParallel for a single file (less ideal):
                int extractedCount = LibBundle3.Index.ExtractParallel(node, tempDir);
                if (extractedCount > 0)
                {
                    // The file will be at tempDir/FileName.ext, not tempDir/relative/path/to/FileName.ext
                    // if ExtractParallel extracts just the file. If it preserves structure, then
                    // tempFilePath = Path.Combine(tempDir, relativePath);
                    // For now, let's assume it extracts with the filename directly into tempDir.
                    string actualExtractedFilePath = Path.Combine(tempDir, fileNode.Name); // Assuming fileNode.Name gives filename
                    if (File.Exists(actualExtractedFilePath))
                    {
                        string content = File.ReadAllText(actualExtractedFilePath, encoding);
                        originalFileCache[relativePath] = content; // Cache it
                        return content;
                    }
                }
                window.EmitToConsole($"Warning: Could not extract original content for {relativePath} using temporary extraction.");
                return null;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        window.EmitToConsole($"Warning: Node not found or not a file in GGPK: {relativePath}");
        return null;
    }

    /// <summary>
    /// Lists files in a GGPK directory matching extensions.
    /// NOTE: This is a placeholder. Actual implementation depends on LibBundle3.Index capabilities.
    /// </summary>
    private IEnumerable<string> ListFilesInGgpKDirectory(string relativeDirectoryPath, string[] extensions)
    {
        List<string> foundFiles = [];
        if (index.TryFindNode(relativeDirectoryPath, out ITreeNode? dirNode) && dirNode.IsDirectory)
        {
            // Assumption: dirNode has a way to access children, e.g., dirNode.Children
            // This part is highly dependent on LibBundle3.ITreeNode actual API.
            // foreach (ITreeNode childNode in dirNode.Children) // Hypothetical
            // {
            //     if (!childNode.IsDirectory && childNode is FileNode fileChildNode) // Hypothetical FileNode
            //     {
            //         string fileName = fileChildNode.Name; // Hypothetical
            //         string fileExtension = Path.GetExtension(fileName);
            //         if (extensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            //         {
            //             // Need the full relative path for the childNode
            //             // string childRelativePath = $"{relativeDirectoryPath.TrimEnd('/')}/{fileName}";
            //             // This assumes dirNode.Children contains nodes with names, not full paths.
            //             // If childNode.Path (hypothetical) gives full relative path:
            //             // foundFiles.Add(childNode.Path);
            //         }
            //     }
            // }

            // If ITreeNode doesn't easily give children or if recursive search is needed and not directly supported:
            // This is a complex part. A robust solution would involve traversing the GGPK index.
            // For now, this will be a major simplification or a point for future improvement.
            // A simpler, less accurate fallback might be to use paths_to_extract.json if it's comprehensive
            // and filter those paths.

            // Let's try to use the existing `paths_to_extract.json` as a temporary workaround
            // if direct GGPK directory listing is too complex for now with LibBundle3.
            // This means patches for directories will only work if those directories/files are in paths_to_extract.json.
            try
            {
                PathData pathData = JsonConvert.DeserializeObject<PathData>(File.ReadAllText(FileExtractor.extractJsonPath))!;
                string normalizedDirPrefix = relativeDirectoryPath.EndsWith("/") ? relativeDirectoryPath : relativeDirectoryPath + "/";

                foreach (string pathFromManifest in pathData.paths)
                {
                    if (pathFromManifest.StartsWith(normalizedDirPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        string fileExtension = Path.GetExtension(pathFromManifest);
                        if (extensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
                        {
                            foundFiles.Add(pathFromManifest);
                        }
                    }
                }
                if (!foundFiles.Any())
                {
                     window.EmitToConsole($"Warning: No files found for directory {relativeDirectoryPath} with extensions {string.Join(", ", extensions)} using manifest fallback.");
                }
            }
            catch (Exception ex)
            {
                window.EmitToConsole($"Error reading {FileExtractor.extractJsonPath} for directory listing: {ex.Message}");
            }
        }
        else
        {
            window.EmitToConsole($"Warning: Directory node not found or not a directory in GGPK: {relativeDirectoryPath}");
        }
        return foundFiles;
    }


    /// <summary>
    /// Main patch method - Refactored for in-memory operations.
    /// </summary>
    public int Patch()
    {
        inMemoryPatchedFiles.Clear(); // Clear from previous run
        originalFileCache.Clear();    // Clear cache from previous run

        Type[] patchTypes = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.GetInterfaces().Contains(typeof(IPatch))).ToArray();
        CollectSettings(); // Collects bools and floats from UI

        IPatch[] patches = patchTypes.Select(pt => (IPatch)Activator.CreateInstance(pt)!)
                                     .Where(p => p.ShouldPatch(bools, floats))
                                     .ToArray();

        if (!patches.Any())
        {
            window.EmitToConsole("No active patches. Nothing to do.");
            return 0;
        }

        // Process files for each patch
        foreach (IPatch patch in patches)
        {
            Stopwatch stopWatch = new();
            stopWatch.Start();
            int filesModifiedByThisPatch = 0;

            // Process individual files
            foreach (string relativePath in patch.FilesToPatch)
            {
                if (ProcessSingleFile(relativePath, patch))
                {
                    filesModifiedByThisPatch++;
                }
            }

            // Process directories
            foreach (string relativeDirectory in patch.DirectoriesToPatch)
            {
                string[] extensions = patch.Extension.Split('|');
                //IEnumerable<string> filesInDir = ListFilesInGgpKDirectory(relativeDirectory, extensions); // IDEAL

                // WORKAROUND: Using CachePath to list files if GGPK listing is hard.
                // This assumes FileExtractor has run and CachePath is populated.
                // This is a temporary workaround and deviates from pure in-memory GGPK operations.
                string fullDirectoryPath = Path.Combine(CachePath, relativeDirectory);
                if (Directory.Exists(fullDirectoryPath))
                {
                    List<string> filesFoundInCacheDir = [];
                    foreach(var ext in extensions)
                    {
                        filesFoundInCacheDir.AddRange(
                            Directory.EnumerateFiles(fullDirectoryPath, $"*{ext}", SearchOption.AllDirectories)
                                     .Select(absPath => absPath.Substring(CachePath.Length).Replace('\\', '/')) // Convert to relative
                        );
                    }

                    foreach (string relativePath in filesFoundInCacheDir)
                    {
                        if (ProcessSingleFile(relativePath, patch))
                        {
                            filesModifiedByThisPatch++;
                        }
                    }
                }
                else
                {
                    window.EmitToConsole($"Warning: Directory {relativeDirectory} (mapped to {fullDirectoryPath}) not found in cache for patch {patch.GetType().Name}. Files in this directory might not be patched.");
                    // Attempt GGPK listing as a fallback if cache isn't there (more aligned with goal but needs robust ListFilesInGgpKDirectory)
                    IEnumerable<string> filesInDirFromGGPK = ListFilesInGgpKDirectory(relativeDirectory, extensions);
                     foreach (string relativePath in filesInDirFromGGPK)
                    {
                        if (ProcessSingleFile(relativePath, patch))
                        {
                            filesModifiedByThisPatch++;
                        }
                    }
                }
            }
            stopWatch.Stop();
            window.EmitToConsole($"{patch.GetType().Name} processed {filesModifiedByThisPatch} file(s) in {(int)stopWatch.Elapsed.TotalMilliseconds}ms.");
        }

        if (!inMemoryPatchedFiles.Any())
        {
            window.EmitToConsole("No files were actually modified by the patches.");
            return 0;
        }

        // Create ZipArchive in memory with the modified files
        string tempZipForLibBundle = Path.Combine(Path.GetTempPath(), "inmemory_patch.zip"); // LibBundle may need a file path
        int count = 0;

        try
        {
            using (MemoryStream zipMemoryStream = new MemoryStream())
            {
                using (ZipArchive archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, true)) // true: leave stream open
                {
                    foreach (var kvp in inMemoryPatchedFiles)
                    {
                        string relativePath = kvp.Key;
                        string content = kvp.Value;

                        ZipArchiveEntry entry = archive.CreateEntry(relativePath.Replace('\\', '/')); // Ensure forward slashes for zip standard
                        using (Stream entryStream = entry.Open())
                        using (StreamWriter writer = new StreamWriter(entryStream, GetEncodingForPath(relativePath)))
                        {
                            writer.Write(content);
                        }
                    }
                } // archive is disposed, zipMemoryStream now contains the zip data.

                // Reset position for reading by the next ZipArchive or by LibBundle3 if it can take a stream
                zipMemoryStream.Position = 0;

                // LibBundle3.Index.Replace expects IReadOnlyCollection<ZipArchiveEntry>.
                // We need to provide this from our memory stream.
                // The simplest way is to write the memory stream to a temporary file if LibBundle3 cannot take a stream directly.
                // Or, open a new ZipArchive on the memory stream for reading.

                // Option 1: Write to temp file (if LibBundle3.Replace needs a file-backed ZipArchive) - current code uses this for patch.zip
                // File.WriteAllBytes(tempZipForLibBundle, zipMemoryStream.ToArray());
                // using (ZipArchive archiveForReading = ZipFile.OpenRead(tempZipForLibBundle))
                // {
                //    count = LibBundle3.Index.Replace(index, archiveForReading.Entries);
                // }
                // File.Delete(tempZipForLibBundle);


                // Option 2: Use ZipArchive on MemoryStream directly for reading (preferred)
                using (ZipArchive archiveForReading = new ZipArchive(zipMemoryStream, ZipArchiveMode.Read, false)) // false: stream can be closed by archive disposal
                {
                     // Ensure all entries are loaded if LibBundle3 is lazy.
                    var entries = archiveForReading.Entries.ToList();
                    if (!entries.Any() && inMemoryPatchedFiles.Any())
                    {
                        window.EmitToConsole("Warning: In-memory zip archive was created but resulted in zero entries for LibBundle3.Replace.");
                         return 0; // Avoids error with LibBundle3 if it expects entries.
                    }
                    window.EmitToConsole($"Applying {entries.Count} modified file(s) to GGPK...");
                    count = LibBundle3.Index.Replace(index, entries);
                }
            }
            window.EmitToConsole($"Successfully applied {count} changes to GGPK.");
        }
        catch (Exception ex)
        {
            window.EmitToConsole($"Error during in-memory zip creation or GGPK replacement: {ex.Message}");
            window.EmitToConsole($"Stack Trace: {ex.StackTrace}");
            // If tempZipForLibBundle was used and an error occurred, try to delete it.
            // if (File.Exists(tempZipForLibBundle)) File.Delete(tempZipForLibBundle);
            return 0; // Indicate failure or no changes applied
        }

        return count;
    }

    private Encoding GetEncodingForPath(string relativePath)
    {
        return Path.GetExtension(relativePath).Equals(".hlsl", StringComparison.OrdinalIgnoreCase) ? Encoding.ASCII : Encoding.Unicode;
    }

    /// <summary>
    /// Processes a single file for a given patch, using in-memory caches.
    /// </summary>
    /// <returns>True if the file was actually modified and stored in inMemoryPatchedFiles by this operation.</returns>
    private bool ProcessSingleFile(string relativePath, IPatch patch)
    {
        string currentContent;
        Encoding fileEncoding = GetEncodingForPath(relativePath);

        if (inMemoryPatchedFiles.TryGetValue(relativePath, out string? patchedContent))
        {
            currentContent = patchedContent;
        }
        else if (originalFileCache.TryGetValue(relativePath, out string? originalCachedContent))
        {
            currentContent = originalCachedContent;
        }
        else
        {
            // Not patched yet this session, and not in original cache - extract from GGPK
            string? extractedContent = ExtractSingleFileToString(relativePath, fileEncoding);
            if (extractedContent == null)
            {
                // window.EmitToConsole($"Skipping {relativePath} for patch {patch.GetType().Name}: Could not extract original content.");
                return false; // Cannot proceed with this file
            }
            currentContent = extractedContent;
            originalFileCache[relativePath] = currentContent; // Cache original
        }

        string? modifiedText = patch.PatchFile(currentContent);

        if (modifiedText != null && modifiedText != currentContent)
        {
            inMemoryPatchedFiles[relativePath] = modifiedText;
            // window.EmitToConsole($"Patched: {relativePath} by {patch.GetType().Name}");
            return true;
        }
        return false;
    }

    // Remove old, disk-based methods:
    // public void ModifyDirectory(string path, string extension, IPatch patch) { ... }
    // public void TryModifyFile(string path, IPatch patch) { ... }
    // public void ModifyFile(string path, IPatch patch) { ... }
    // The public HashSet<string> patchedFiles is also no longer used in the same way.
}

// Helper class for paths_to_extract.json deserialization (if not already defined elsewhere)
// This might need to be in its own file or a shared location if used by FileExtractor too.
// Assuming it's defined in FileExtractor.cs or similar, so not redefining here.
// public class PathData { public List<string> paths { get; set; } }