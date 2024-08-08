namespace PoeFixer;

public class LoadingScreenPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => ["metadata/ui/loadingstate/"];

    public string Extension => "*.ui";

    public string? PatchFile(string text)
    {
        List<string> lines = [.. text.Split("\r\n")];

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("begin"))
            {
                lines.Insert(i + 1, "\tvisible = false;");
                lines.Insert(i + 1, "\tmouse_enabled = false;");
            }
        }

        return string.Join("\r\n", lines);
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        return bools.TryGetValue("removeLoadingScreen", out bool value) && value;
    }
}
