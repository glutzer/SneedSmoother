namespace PoeFixer;

public class BloomPatch : IPatch
{
    public string[] FilesToPatch => [
        "shaders/bloomgather.hlsl"
        ];

    public string[] DirectoriesToPatch => [];

    public string Extension => "*";

    public string? PatchFile(string text)
    {
        string searchString = "float4 BloomGather";
        int index = text.IndexOf(searchString);
        string newContent = text[..(index + searchString.Length)];

        return newContent + "( PInput input ) : PIXEL_RETURN_SEMANTIC\r\n{\r\n\treturn 0;\r\n}\r\n";
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("removeBloom", out bool enabled);
        return enabled;
    }
}