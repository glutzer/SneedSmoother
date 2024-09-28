namespace PoeFixer
{
    public class RevealPatch : IPatch
    {
        // Files to patch
        public string[] FilesToPatch => new[] 
        { 
            "shaders/minimap_visibility_pixel.hlsl", 
            "shaders/minimap_blending_pixel.hlsl" 
        };

        public string[] DirectoriesToPatch => new string[] { };
        public string Extension => "*";

        // Check if the patch should be applied based on the revealEnabled flag
        public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
        {
            bools.TryGetValue("revealEnabled", out bool revealEnabled);
            return revealEnabled; // Patch if revealEnabled is true
        }

        // Apply the appropriate patch (assuming file content type will help determine what to patch)
        public string? PatchFile(string text)
        {
            // Apply patch logic without depending on the file name
            if (text.Contains("float4(0.0f, 0.0f, 0.0f, 1.0f)"))
            {
                return text.Replace("float4(0.0f, 0.0f, 0.0f, 1.0f)", "float4(0.18f, 0.0f, 0.0f, 1.0f)");
            }
            else if (text.Contains("0.00f, 0.00f, 0.00f, 0.3f"))
            {
                // Replace first color
                text = text.Replace("0.00f, 0.00f, 0.00f, 0.3f", "1.0f, 1.0f, 1.0f, 0.01f");
                // Replace second color
                text = text.Replace("12.00f, 12.00f, 12.00f, 0.1f", "0.5f, 0.5f, 1.0f, 0.5f");
                return text;
            }

            // Return original text if no patch is applied
            return text;
        }
    }
}
