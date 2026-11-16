using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Optionary.Build.Tasks;

/// <summary>
///     Turns <c>OptionarySchemaFragment</c> items into <c>AssemblyMetadata</c> items. An item's <c>Include</c> is the section path,
///     and exactly one of its <c>Schema</c> (inline JSON) or <c>File</c> (a JSON file) metadata holds the schema of the section's
///     value. Each becomes <c>Optionary.Schema.v1:&lt;section path&gt;</c> with the JSON, compacted to one line, as its
///     <c>Value</c>, after it has been validated as a v1 fragment, the format the merge reads.
/// </summary>
/// <remarks>
///     Errors: OPT1002 (neither or both of <c>Schema</c> and <c>File</c>), OPT1003 (an empty section path or segment, the
///     generator's OPT0003 rules), OPT1004 (the file doesn't exist or can't be read) and OPT1005 (not valid JSON, not an object,
///     or an invalid v1 fragment).
/// </remarks>
[PublicAPI]
public sealed class ReadConfigurationSchemaFragmentItems : Microsoft.Build.Utilities.Task
{
    private const string ItemName = "OptionarySchemaFragment";

    /// <summary>
    ///     The <c>OptionarySchemaFragment</c> items.
    /// </summary>
    [Required]
    public ITaskItem[] Fragments { get; set; } = [];

    /// <summary>
    ///     The project's directory, which relative <c>File</c> paths are relative to.
    /// </summary>
    [Required]
    public string ProjectDirectory { get; set; } = "";

    /// <summary>
    ///     One <c>AssemblyMetadata</c> item per fragment, its value escaped for MSBuild.
    /// </summary>
    [Output]
    public ITaskItem[] AssemblyMetadata { get; set; } = [];

    /// <summary>
    ///     The full paths of the <c>File</c> items read, for the compiler's incremental inputs.
    /// </summary>
    [Output]
    public ITaskItem[] Files { get; set; } = [];

    /// <inheritdoc />
    public override bool Execute()
    {
        var metadata = new List<ITaskItem>();
        var files = new List<ITaskItem>();
        foreach (var item in Fragments)
        {
            if (Read(item, files) is { } fragment) metadata.Add(fragment);
        }

        AssemblyMetadata = [.. metadata];
        Files = [.. files];
        return !Log.HasLoggedErrors;
    }

    private TaskItem? Read(ITaskItem item, List<ITaskItem> files)
    {
        // Not FullPath or other path metadata: the item spec is a section path, which needn't be a valid file name.
        var project = item.GetMetadata("DefiningProjectFullPath");
        var section = item.ItemSpec;
        if (InvalidSectionReason(section) is { } reason)
        {
            Error("OPT1003", project, "The {0} item \"{1}\" isn't a valid section path: {2}.", ItemName, section, reason);
            return null;
        }

        var inline = item.GetMetadata("Schema");
        var file = item.GetMetadata("File");
        if (inline.Trim().Length == 0 == ( file.Trim().Length == 0 ))
        {
            Error(
                "OPT1002",
                project,
                "The {0} item \"{1}\" has {2}: set exactly one, Schema to the section's schema as JSON or File to a JSON file holding it.",
                ItemName,
                section,
                inline.Trim().Length == 0 ? "neither Schema nor File metadata" : "both Schema and File metadata"
            );
            return null;
        }

        string json;
        string source;
        string location;
        if (file.Trim().Length > 0)
        {
            var path = Path.GetFullPath(Path.Combine(ProjectDirectory, file.Trim()));
            source = $"file \"{path}\"";
            location = path;
            if (!File.Exists(path))
            {
                Error("OPT1004", project, "The {0} item \"{1}\" has the file \"{2}\", which doesn't exist.", ItemName, section, path);
                return null;
            }

            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Error("OPT1004", project, "The {0} item \"{1}\" has the file \"{2}\", which can't be read: {3}", ItemName, section, path, exception.Message);
                return null;
            }

            files.Add(new TaskItem(Escape(path)));
        }
        else
        {
            json = inline;
            source = "Schema metadata";
            location = project;
        }

        var key = ConfigurationSchemaFragment.VersionPrefix + section;
        var fragment = ConfigurationSchemaFragment.TryParse(key, json, ItemName, out var problem);
        if (fragment is not null && fragment.Schema is not JsonObject)
        {
            fragment = null;
            problem = "the schema is not a JSON object";
        }

        if (fragment is null)
        {
            Error("OPT1005", location, "The {0} item \"{1}\": its {2} isn't a configuration schema fragment: {3}.", ItemName, section, source, problem!);
            return null;
        }

        Log.LogMessage(MessageImportance.Low, "Optionary: configuration schema fragment \"{0}\" from its {1}", section, source);
        var result = new TaskItem(Escape(key));
        // TaskItem stores metadata escaped and unescapes it on the way out: escape it, so the JSON reaches the attribute verbatim.
        result.SetMetadata("Value", Escape(fragment.Json));
        return result;
    }

    // The generator's rules for an explicit section path (OPT0003).
    private static string? InvalidSectionReason(string path) =>
        path.Trim().Length == 0
            ? "section paths can't be empty"
            : path.Split(':').Any(z => z.Trim().Length == 0)
                ? "section paths can't have empty segments"
                : null;

    private void Error(string code, string file, string message, params object[] arguments) =>
        Log.LogError(null, code, null, file, 0, 0, 0, 0, message, arguments);

    /// <summary>
    ///     MSBuild's escaping (<c>%XX</c> for <c>% * ? @ $ ( ) ; '</c>), which <c>Microsoft.Build.Utilities</c> doesn't expose.
    /// </summary>
    internal static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is '%' or '*' or '?' or '@' or '$' or '(' or ')' or ';' or '\'')
            {
                builder.Append('%').Append(( (int)character ).ToString("X2", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
