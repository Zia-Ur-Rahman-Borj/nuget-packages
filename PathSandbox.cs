namespace FileAppendMcp;

/// <summary>
/// Confines every file operation to one allowed folder. Without this, the model
/// could be talked into reading or editing anything your Windows account can reach.
/// </summary>
public sealed class PathSandbox
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public string Root { get; }

    public PathSandbox(string allowedRoot)
    {
        var full = Path.GetFullPath(allowedRoot);

        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException($"Allowed folder does not exist: {full}");

        // A whole drive defeats the point of a sandbox.
        if (string.Equals(Path.GetPathRoot(full), full, PathComparison))
            throw new ArgumentException("Pick a folder, not a drive root.");

        Root = Path.TrimEndingDirectorySeparator(full);
    }

    /// <summary>
    /// Resolves a path (relative paths start from the allowed folder) and returns the
    /// full path of an EXISTING file inside it. Throws if anything looks wrong.
    /// </summary>
    public string ResolveExistingFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.");

        // GetFullPath collapses "..\" tricks before we check containment.
        var full = Path.GetFullPath(path, Root);

        if (!full.StartsWith(Root + Path.DirectorySeparatorChar, PathComparison))
            throw new UnauthorizedAccessException("Path is outside the allowed folder.");

        if (!File.Exists(full))
            throw new FileNotFoundException("File not found.");

        // Reject symlinks/junctions on the file or any folder between it and the
        // root, since a link can quietly point outside the allowed folder.
        if (File.GetAttributes(full).HasFlag(FileAttributes.ReparsePoint))
            throw new UnauthorizedAccessException("Links are not allowed.");

        for (var dir = new DirectoryInfo(Path.GetDirectoryName(full)!);
             dir is not null && dir.FullName.Length > Root.Length;
             dir = dir.Parent)
        {
            if (dir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new UnauthorizedAccessException("Links are not allowed.");
        }

        return full;
    }
}
