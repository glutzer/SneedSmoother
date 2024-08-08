namespace PoeFixer;

public class ZoomPatch : IPatch
{
    public float zoomLevel = 1;

    public string Extension => "*.ot|*.otc";

    public string[] FilesToPatch => [
        "metadata/characters/character.ot",
        "metadata/miscellaneousobjects/blackstararena/blackstar.otc",
        "metadata/monsters/thecollector/thecollectorbase.otc",
        "metadata/npc/league/ancestral/hinekora.otc",
        "metadata/questobjects/epicdoor/epicdoor.otc",
        "metadata/terrain/leagues/harvest/objects/soultree.otc",
        "metadata/terrain/leagues/settlers/objects/zoom_node.otc",
        "metadata/terrain/missions/hideouts/atlashideout/objects/optdevicezoom.otc",
        "metadata/terrain/testareas/programming/objects/zoom_node.otc"
        ];

    public string[] DirectoriesToPatch => ["metadata/miscellaneousobjects/camerazoom/"];

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("zoomEnabled", out bool enabled);
        floats.TryGetValue("zoomSlider", out zoomLevel);
        return enabled;
    }

    public string? PatchFile(string text)
    {
        // Not working, not sure what zooms the camera in.

        if (text.Contains("CreateCameraZoomNode") || text.Contains("ClearCameraZoomNodes") || text.Contains("CreateCameraLookAtNode"))
        {
            // Remove other zoom nodes.
            // ClearCameraZoomNodes CreateCameraLookAtNode.
            List<string> lines = text.Split("\r\n").Where(x => !x.Contains("ClearCameraZoomNodes") && !x.Contains("CreateCameraLookAtNode")).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("CreateCameraZoomNode"))
                {
                    int start = lines[i].IndexOf("CreateCameraZoomNode");

                    int end = lines[i].IndexOf(')', start);

                    lines[i] = lines[i][..start] + $"CreateCameraZoomNode(1000000, 1000000, {zoomLevel})" + lines[i][(end + 1)..];
                }
            }

            return string.Join("\r\n", lines);
        }
        else
        {
            // Create character zoom node.

            List<string> lines = [.. text.Split("\r\n")];

            int index = lines.FindIndex(x => x.Contains("team = 1"));

            if (index == -1) return text;

            lines.Insert(index + 1, $"\ton_initial_position_set = \"CreateCameraZoomNode(1000000, 1000000, {zoomLevel});\"");

            return string.Join("\r\n", lines);
        }
    }
}