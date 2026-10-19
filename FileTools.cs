using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;

namespace FileAppendMcp;

[McpServerToolType]
public sealed class FileTools
{
    private const long MaxReadBytes = 1_000_000; // keeps huge files out of the model's context

    private readonly PathSandbox _sandbox;

    public FileTools(PathSandbox sandbox) => _sandbox = sandbox;

    [McpServerTool(Name = "read_file"),
     Description("Reads a text file inside the allowed folder and returns its full contents. " +
                 "Use a path relative to the allowed folder, for example 'notes/todo.txt'.")]
    public string ReadFile(
        [Description("Path of the file, relative to the allowed folder")] string path)
    {
        try
        {
            var full = _sandbox.ResolveExistingFile(path);

            var length = new FileInfo(full).Length;
            if (length > MaxReadBytes)
                return $"Error: file is {length:N0} bytes; the limit is {MaxReadBytes:N0}.";

            var text = File.ReadAllText(full);
            return text.Length == 0 ? "(file is empty)" : text;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return $"Error: {ex.Message}";
        }
    }

    [McpServerTool(Name = "append_line"),
     Description("Appends one line of text to the end of an existing text file inside the allowed " +
                 "folder and saves it. Never creates files and never overwrites existing content.")]
    public string AppendLine(
        [Description("Path of the file, relative to the allowed folder")] string path,
        [Description("The single line to append (no line breaks)")] string line)
    {
        if (line is null || line.Contains('\n') || line.Contains('\r'))
            return "Error: 'line' must be a single line of text with no line breaks.";

        try
        {
            var full = _sandbox.ResolveExistingFile(path);

            using var fs = new FileStream(full, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

            // If the file doesn't already end with a newline, add one first so the
            // new line isn't glued onto the previous one.
            var needsLeadingNewline = false;
            if (fs.Length > 0)
            {
                fs.Seek(-1, SeekOrigin.End);
                needsLeadingNewline = fs.ReadByte() != '\n';
            }

            fs.Seek(0, SeekOrigin.End);
            var text = (needsLeadingNewline ? Environment.NewLine : "") + line + Environment.NewLine;
            fs.Write(new UTF8Encoding(false).GetBytes(text));
            fs.Flush(true); // push to disk so the change is saved right away

            return $"Appended 1 line to '{path}'. File is now {fs.Length:N0} bytes.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return $"Error: {ex.Message}";
        }
    }
}
