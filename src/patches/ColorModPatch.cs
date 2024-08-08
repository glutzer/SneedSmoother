using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace PoeFixer;

public class ColorModPatch : IPatch
{
    public string[] FilesToPatch => [];

    public string[] DirectoriesToPatch => ["metadata/statdescriptions/"];

    public string Extension => "*.txt";

    public enum ReadState
    {
        WritingData,
        ReadingData,
        ReadingDescription,
        ReadingToDescription
    }

    public string? PatchFile(string text)
    {
        ColorModInfo colorModInfo = JsonConvert.DeserializeObject<ColorModInfo>(File.ReadAllText("color_mods.json"))!;

        string[] lines = text.Split("\r\n");

        ReadState state = ReadState.ReadingToDescription;

        string? currentAnnotation = null;

        int linesToWrite = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.StartsWith("description"))
            {
                state = ReadState.ReadingDescription;
                continue;
            }

            if (state == ReadState.ReadingToDescription) continue;

            // Read the description on the next line.
            if (state == ReadState.ReadingDescription)
            {
                string[] description = line.Split(' ');
                if (description.Length < 2)
                {
                    state = ReadState.ReadingToDescription;
                    continue;
                }

                string modType = description[1];

                if (colorModInfo.annotations.TryGetValue(modType, out string? annotation))
                {
                    currentAnnotation = annotation;
                    state = ReadState.ReadingData;
                }
                else
                {
                    currentAnnotation = null;
                    state = ReadState.ReadingToDescription;
                }

                continue;
            }

            if (state == ReadState.ReadingData)
            {
                // Replace tabs in line with nothing.
                string firstNumber = line.Replace("\t", "").Split(' ')[0];

                // May be a "lang" value. If the value is a number write those lines.
                if (int.TryParse(firstNumber, out int value))
                {
                    state = ReadState.WritingData;
                    linesToWrite = value;
                    continue;
                };
            }

            if (state == ReadState.WritingData)
            {
                if (line.Contains('<')) // Already annotated.
                {
                    // Replace text between brackets with new annotation.

                    string pattern = "<.*?>";
                    string replacement = Regex.Replace(line, pattern, new MatchEvaluator(match =>
                    {
                        return $"<{currentAnnotation}>";
                    }));
                    lines[i] = replacement;
                }
                else
                {
                    // Surround the value with the annotation.
                    // "value" -> "<annotation>{{value}}".

                    string pattern = "\".*?\"";
                    string replacement = Regex.Replace(line, pattern, new MatchEvaluator(match =>
                    {
                        return $"\"<{currentAnnotation}>{{{{{match.Value.Replace("\"", "")}}}}}\"";
                    }));
                    lines[i] = replacement;
                }

                linesToWrite--;
                if (linesToWrite == 0)
                {
                    state = ReadState.ReadingData;
                    continue;
                }
            }
        }

        return string.Join("\r\n", lines);
    }

    public bool ShouldPatch(Dictionary<string, bool> bools, Dictionary<string, float> floats)
    {
        bools.TryGetValue("colorModsEnabled", out bool enabled);

        return enabled;
    }
}

/// <summary>
/// Info about color mods imported from json.
/// </summary>
public class ColorModInfo
{
    public Dictionary<string, string> annotations = [];
}