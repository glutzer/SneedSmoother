namespace PoeFixer;

public class ShatterPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => [
        "metadata/effects/spells/shatter"
        ];

    public string Extension => "*.otc";

    public string? PatchFile(string text)
    {
        return text.Replace("Render\r\n{\r\n}", "Render\r\n{\r\n\tdisable_rendering = true\r\n}");
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeShatter", out bool enabled);
        return enabled;
    }
}
