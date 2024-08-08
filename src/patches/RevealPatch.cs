namespace PoeFixer;

public class RevealPatch : IPatch
{
    public string[] FilesToPatch => ["shaders/minimap_visibility_pixel.hlsl"];
    public string[] DirectoriesToPatch => [];
    public string Extension => "*";

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("revealEnabled", out bool enabled);
        return enabled;
    }

    public string? PatchFile(string text)
    {
        return text.Replace("float4(0.0f, 0.0f, 0.0f, 1.0f)", "float4(0.18f, 0.0f, 0.0f, 1.0f)");
    }
}