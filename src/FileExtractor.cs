using LibBundle3.Nodes;
using Newtonsoft.Json;
using System.IO;

namespace PoeFixer;

public class FileExtractor
{
    public LibBundle3.Index index;

    public const string extractJsonPath = "paths_to_extract.json";

    public FileExtractor(LibBundle3.Index index)
    {
        this.index = index;
    }

    /// <summary>
    /// Extracts vanilla assets.
    /// </summary>
    /// <returns>Number of files extracted.</returns>
    public int ExtractFiles()
    {
        string cachePath = $"{AppDomain.CurrentDomain.BaseDirectory}extractedassets/";

        if (Directory.Exists(cachePath)) Directory.Delete(cachePath, true);

        PathData pathData = JsonConvert.DeserializeObject<PathData>(File.ReadAllText(extractJsonPath))!;

        int count = 0;

        foreach (string path in pathData.paths)
        {
            index.TryFindNode(path, out ITreeNode? node);
            if (node == null) continue;

            string directory = Path.GetDirectoryName(path)!;

            count += LibBundle3.Index.ExtractParallel(node, $"{cachePath}{directory}");
        }

        return count;
    }
}