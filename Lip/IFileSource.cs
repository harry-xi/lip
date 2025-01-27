namespace Lip;

/// <summary>
/// File source is a unified interface for file access for different file structures.
/// </summary>
public interface IFileSource
{
    /// <summary>
    /// Adds a new entry to the file source.
    /// </summary>
    /// <param name="key">The unique identifier for the entry.</param>
    /// <param name="stream">The data stream containing the entry's content.</param>
    /// <returns>A new <see cref="IFileSourceEntry"/> representing the added entry.</returns>
    Task<IFileSourceEntry> AddEntry(string key, Stream stream);

    /// <summary>
    /// Retrieves an entry from the file source by its key.
    /// </summary>
    /// <param name="key">The unique identifier of the entry to retrieve.</param>
    /// <returns>The entry if found; otherwise, null.</returns>
    Task<IFileSourceEntry?> GetEntry(string key);

    /// <summary>
    /// Removes an entry from the file source.
    /// </summary>
    /// <param name="key">The unique identifier of the entry to remove.</param>
    Task RemoveEntry(string key);
}

/// <summary>
/// Represents an entry within a file source.
/// </summary>
public interface IFileSourceEntry
{
    /// <summary>
    /// Gets a value indicating whether this entry represents a directory.
    /// </summary>
    /// <value><c>true</c> if the entry is a directory; otherwise, <c>false</c>.</value>
    bool IsDirectory { get; }

    /// <summary>
    /// Gets the unique identifier of this entry.
    /// </summary>
    /// <value>The entry's key within the file source.</value>
    string Key { get; }

    /// <summary>
    /// Opens a stream to access the entry's content.
    /// </summary>
    /// <returns>A stream containing the entry's data.</returns>
    Task<Stream> OpenEntryStream();
}
