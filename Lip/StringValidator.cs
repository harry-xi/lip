using System.Text.RegularExpressions;
using Semver;

namespace Lip;

public static partial class StringValidator
{
    /// <summary>
    /// Checks if the script name is valid.
    /// </summary>
    /// <param name="scriptName">The script name to validate.</param>
    /// <returns>True if the script name is valid; otherwise, false.</returns>
    public static bool IsScriptNameValid(string scriptName)
    {
        return ScriptNameGeneratedRegex().IsMatch(scriptName);
    }

    /// <summary>
    /// Checks if the tag is valid.
    /// </summary>
    /// <param name="tag">The tag to validate.</param>
    /// <returns>True if the tag is valid; otherwise, false.</returns>
    public static bool IsTagValid(string tag)
    {
        return TagGeneratedRegex().IsMatch(tag);
    }

    /// <summary>
    /// Checks if the version is valid.
    /// </summary>
    /// <param name="version">The version to validate.</param>
    /// <returns>True if the version is valid; otherwise, false.</returns>
    public static bool IsVersionValid(string version)
    {
        return SemVersion.TryParse(version, out _);
    }

    [GeneratedRegex("^[a-z0-9]+(_[a-z0-9]+)*$")]
    private static partial Regex ScriptNameGeneratedRegex();

    [GeneratedRegex("^[a-z0-9-]+(:[a-z0-9-]+)?$")]
    private static partial Regex TagGeneratedRegex();
}
