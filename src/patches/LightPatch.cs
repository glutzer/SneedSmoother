namespace PoeFixer;

public class LightPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => [
        "metadata/environmentsettings"
        ];

    public string Extension => "*.env";

    public string? PatchFile(string text)
    {
        text = text.Replace("directional_light", "xirectional_light")
            .Replace("player_light", "xlayer_light")
            .Replace("environment_mapping", "xnvironment_mapping")
            .Replace("global_illumination", "xlobal_illumination");
        return text;
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeLight", out bool enabled);
        return enabled;
    }
}