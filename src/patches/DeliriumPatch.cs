namespace PoeFixer;

public class DeliriumPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => [
        "metadata/effects/environment/league_affliction"
        ];


    public string Extension => "*.ao|*.aoc";

    public string? PatchFile(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        if (text.Contains("Metadata/FmtParent") && !text.Contains("AnimatedRender"))
        {
            text = "version 2\nextends \"Metadata/FmtParent\"";
        }
        else if (text.Contains("Metadata/FmtParent") && text.Contains("AnimatedRender"))
        {
            text = "version 2\nextends \"Metadata/FmtParent\"\n\nAnimatedRender\n{\n\tcannot_be_disabled = true\n}";
        }
        else if (text.Contains("default_animation = \"loop\""))
        {
            string[] separator = [Environment.NewLine];
            string[] lines = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<string> filteredLines = lines.Where(line => !line.Contains("default_animation = \"loop\""));
            text = string.Join(Environment.NewLine, filteredLines);
        }
        else if (text.Contains("BoneGroups"))
        {
            text = @"version 2
extends ""Metadata/Parent""

ClientAnimationController
{
	skeleton = ""Art/Models/Effects/enviro_effects/weather_attachments/generic_rig/weather_rig.ast""
}

BoneGroups
{
	bone_group = ""box false aux_box1 aux_box2 aux_box3 ""
}";
        }

        return text;
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeDelirium", out bool enabled);
        return enabled;
    }
}