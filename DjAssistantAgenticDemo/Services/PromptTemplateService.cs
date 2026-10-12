using System.IO;

namespace DjAssistantAgenticDemo.Services;

/// <summary>
/// Service for loading and managing prompt templates from YAML files in the Promts folder.
/// Shared utility for all agent services that use Handlebars-based prompts.
/// </summary>
public static class PromptTemplateService
{
    private const string PromptsFolder = "Prompts";

    /// <summary>
    /// Loads system and user prompt templates from a YAML file in the Promts folder.
    /// </summary>
    /// <param name="fileName">Name of the YAML file (e.g., "GapFillerPromts.yaml")</param>
    /// <returns>Tuple containing (system, user) prompt templates</returns>
    /// <exception cref="FileNotFoundException">Thrown when YAML file cannot be found</exception>
    /// <exception cref="InvalidOperationException">Thrown when prompts are missing from YAML</exception>
    public static (string system, string user) LoadPromptTemplates(string fileName)
    {
        // Search for Promts folder
        string relativePath = Path.Combine(PromptsFolder, fileName);
        string? file = FindFileUpwards(relativePath, AppContext.BaseDirectory, 6);

        if (file == null || !File.Exists(file))
        {
            throw new FileNotFoundException($"{fileName} not found in {PromptsFolder} folder. Full path searched: {Path.Combine(AppContext.BaseDirectory, relativePath)}");
        }

        var text = File.ReadAllText(file);
        var lines = text.Replace("\r\n", "\n").Split('\n');

        string ExtractBlock(string key)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + ":"))
                {
                    // block scalar expected (|)
                    int j = i + 1;
                    var sb = new System.Text.StringBuilder();
                    for (; j < lines.Length; j++)
                    {
                        var line = lines[j];
                        if (line.Length >= 2 && (line.StartsWith("  ") || line.Trim().Length == 0))
                        {
                            // remove two-space indent if present
                            sb.AppendLine(line.Length >= 2 ? line.Substring(Math.Min(2, line.Length)) : string.Empty);
                        }
                        else
                        {
                            break;
                        }
                    }
                    return sb.ToString().TrimEnd('\n', '\r');
                }
            }
            return string.Empty;
        }

        var system = ExtractBlock("system");
        var user = ExtractBlock("user");

        if (string.IsNullOrWhiteSpace(system) || string.IsNullOrWhiteSpace(user))
        {
            throw new InvalidOperationException($"system and user prompts must be defined in {fileName}");
        }

        return (system, user);
    }

    /// <summary>
    /// Finds a file by searching upwards through parent directories.
    /// </summary>
    /// <param name="relativePath">Relative path to search for</param>
    /// <param name="startDirectory">Starting directory for the search</param>
    /// <param name="maxLevels">Maximum number of parent directories to traverse</param>
    /// <returns>Full path to the file if found, null otherwise</returns>
    private static string? FindFileUpwards(string relativePath, string startDirectory, int maxLevels)
    {
        var dir = new DirectoryInfo(startDirectory);
        for (int i = 0; i < maxLevels && dir != null; i++)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}
