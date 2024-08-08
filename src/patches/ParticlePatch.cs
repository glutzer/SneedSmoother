namespace PoeFixer;

public class ParticlePatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => ["metadata/particles/"];

    public string Extension => "*.pet|*.trl";

    public string? PatchFile(string text)
    {
        int lengthOfFirstLine = text.IndexOf("\r\n");

        if (lengthOfFirstLine == -1) return null;

        string newText = "0" + text.Substring(lengthOfFirstLine);

        return newText;
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        return bools.TryGetValue("removeParticles", out bool value) && value;
    }
}