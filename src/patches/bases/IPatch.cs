namespace PoeFixer;

public interface IPatch
{
    string[] FilesToPatch { get; }
    string[] DirectoriesToPatch { get; }
    string Extension { get; }
    bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats);
    string? PatchFile(string text);
}
