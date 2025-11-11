// <copyright file="SingleThreadedChecksumCalculator.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Test1;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Provides single-threaded asynchronous methods for computing checksums of directories and files
/// based on a specific hashing algorithm that considers both names and content.
/// The checksum calculation follows these rules:
/// - For files: MD5(file_name + file_content)
/// - For directories: MD5(directory_name + checksum_of_child1 + checksum_of_child2 + ...)
/// All names are converted to bytes using UTF8 encoding.
/// </summary>
public static class SingleThreadedChecksumCalculator
{
    private const int BufferSize = 81920; // 80KB buffer for file reading

    /// <summary>
    /// Computes the checksum for the specified directory and all its contents recursively.
    /// </summary>
    /// <param name="directoryPath">The path to the directory for which to compute the checksum.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.
    /// The task result contains the MD5 checksum as a byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="directoryPath"/> is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the cancellation token.</exception>
    public static async Task<byte[]> ComputeDirectoryChecksumAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentNullException(nameof(directoryPath));
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        return await ComputeChecksumRecursiveAsync(directoryPath, cancellationToken);
    }

    private static async Task<byte[]> ComputeChecksumRecursiveAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (File.Exists(path))
        {
            return await ComputeFileChecksumAsync(path, cancellationToken);
        }

        if (Directory.Exists(path))
        {
            return await ComputeDirectoryChecksumInternalAsync(path, cancellationToken);
        }

        throw new ArgumentException($"Path does not exist: {path}");
    }

    private static async Task<byte[]> ComputeFileChecksumAsync(string filePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        var fileName = Path.GetFileName(filePath);
        var fileNameBytes = Encoding.UTF8.GetBytes(fileName);

        using var md5 = MD5.Create();
        md5.TransformBlock(fileNameBytes, 0, fileNameBytes.Length, fileNameBytes, 0);
        await using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var buffer = new byte[BufferSize];
        int bytesRead;
        while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            md5.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        md5.TransformFinalBlock([], 0, 0);
        var hash = md5.Hash;
        return hash ?? throw new InvalidOperationException("MD5 hash computation returned null.");
    }

    private static async Task<byte[]> ComputeDirectoryChecksumInternalAsync(string directoryPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentNullException(nameof(directoryPath));
        }

        var directoryName = Path.GetFileName(directoryPath);
        if (string.IsNullOrEmpty(directoryName))
        {
            directoryName = directoryPath;
        }

        var directoryNameBytes = Encoding.UTF8.GetBytes(directoryName);

        var entries = new List<string>();

        entries.AddRange(Directory.GetDirectories(directoryPath));
        entries.AddRange(Directory.GetFiles(directoryPath));

        entries.Sort(StringComparer.Ordinal);
        var childChecksums = new List<byte[]>();
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var childChecksum = await ComputeChecksumRecursiveAsync(entry, cancellationToken);
            childChecksums.Add(childChecksum);
        }

        using var md5 = MD5.Create();

        md5.TransformBlock(directoryNameBytes, 0, directoryNameBytes.Length, null, 0);
        foreach (var checksum in childChecksums)
        {
            md5.TransformBlock(checksum, 0, checksum.Length, null, 0);
        }

        md5.TransformFinalBlock([], 0, 0);
        var hash = md5.Hash;
        return hash ?? throw new InvalidOperationException("MD5 hash computation returned null.");
    }
}