namespace PoeFixer;

public class ShadowPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => [
        "metadata/environmentsettings"
        ];

    public string Extension => "*.env";

    public string? PatchFile(string text)
    {
        text = text.Replace("\"shadows_enabled\": true", "\"shadows_enabled\": false");
        return text;
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeShadows", out bool enabled);
        return enabled;
    }
}
