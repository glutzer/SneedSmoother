namespace PoeFixer;

public class CorpsePatch : IPatch
{
    public string[] FilesToPatch => [
        "metadata/monsters/monster.ot"
        ];

    public string[] DirectoriesToPatch => [];

    public string Extension => "*";

    public string? PatchFile(string text)
    {
        text = text.Replace("Life\r\n{\r\n}", "Life\r\n{\r\n\ton_spawned_dead = \"RemoveEffects(); DisableRendering();\"\r\n\ton_death = \"RemoveEffects(); DisableRendering();\"\r\n}");

        string originalText = "slow_animations_go_to_idle = true\r\n}";
        string toReplace = "slow_animations_go_to_idle = true\r\n\ton_start_Revive = \"RemoveEffects(); EnableRendering();\"\r\n}";

        return text.Replace(originalText, toReplace);
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeCorpses", out bool enabled);
        return enabled;
    }
}