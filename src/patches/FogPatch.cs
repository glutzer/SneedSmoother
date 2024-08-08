namespace PoeFixer;

public class FogPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => [
        "metadata/environmentsettings"
        ];

    public string Extension => "*.env";

    public string? PatchFile(string text)
    {
        text = text.Replace("area", "xrea");
        text = text.Replace("fog", "xog");
        return text;
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeFog", out bool enabled);
        return enabled;
    }
}