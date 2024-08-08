namespace PoeFixer.src.patches;

public class MutePatches : IPatch
{
    public string[] FilesToPatch => [
        "metadata/pet/goblinband/goblinbandleader.aoc"
        ];

    public string[] DirectoriesToPatch => ["metadata/pet/babybosseshumans/"];

    public string Extension => "*.aoc";

    public string? PatchFile(string text)
    {
        // Remove all text after SoundEvents.
        int index = text.IndexOf("SoundEvents");
        if (index == -1) return text;
        return text[..index];
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        return bools.TryGetValue("removeGoblins", out bool value) && value;
    }
}