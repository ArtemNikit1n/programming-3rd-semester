// <copyright file="Client.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Client;

using System.Net.Sockets;

/// <summary>
/// TCP client for interacting with file server that supports directory listing and file download operations.
/// Implements IDisposable for proper resource cleanup.
/// </summary>
public class Client : IDisposable
{
    private const string DirectoryNotFoundResponse = "-1";
    private const string BadRequestCode = "400";
    private const string ForbiddenCode = "403";
    private const string InternalServerErrorCode = "500";

    private readonly TcpClient? client;
    private readonly NetworkStream? stream;
    private readonly StreamReader? reader;
    private readonly StreamWriter? writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// Private constructor to prevent direct instantiation.
    /// Use <see cref="CreateAndConnectAsync"/> instead.
    /// </summary>
    private Client(TcpClient client, NetworkStream stream, StreamReader reader, StreamWriter writer)
    {
        this.client = client;
        this.stream = stream;
        this.reader = reader;
        this.writer = writer;
    }

    /// <summary>
    /// Creates and connects a new Client instance.
    /// </summary>
    /// <param name="host">Server hostname or IP address (default: localhost).</param>
    /// <param name="port">Server port number (default: 8080).</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A connected <see cref="Client"/> instance.</returns>
    /// <exception cref="SocketException">Thrown when network connection fails.</exception>
    /// <exception cref="IOException">Thrown when connection setup fails.</exception>
    public static async Task<Client> CreateAndConnectAsync(
        string host = "localhost",
        int port = 8080,
        CancellationToken cancellationToken = default)
    {
        var tcpClient = new TcpClient();
        try
        {
            await tcpClient.ConnectAsync(host, port, cancellationToken);
            var stream = tcpClient.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream);

            return new Client(tcpClient, stream, reader, writer);
        }
        catch
        {
            tcpClient.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Retrieves directory contents from the server.
    /// </summary>
    /// <param name="path">Directory path relative to server's working directory.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// ListResult containing directory contents or error information.
    /// Files and directories with spaces in names are filtered out by the server.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when client is not connected.</exception>
    /// <remarks>
    /// Request format: "1 {path}"
    /// Response format: "size name1 isDir1 name2 isDir2 ..."
    /// Special response: "-1" when directory doesn't exist.
    /// </remarks>
    public async Task<ListResult> ListDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        if (this.reader == null || this.writer == null)
        {
            throw new InvalidOperationException("Client not connected");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ListResult { Success = false, ErrorMessage = $"{BadRequestCode} Bad Request: Path cannot be empty" };
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return new ListResult { Success = false, ErrorMessage = $"{BadRequestCode} Bad Request: Invalid path characters" };
        }

        await this.writer.WriteLineAsync($"1 {path}");
        await this.writer.FlushAsync(cancellationToken);

        var response = await this.reader.ReadLineAsync(cancellationToken);
        return response == null ? new ListResult { Success = false, ErrorMessage = "No response from server" } : ParseListResponse(response);
    }

    /// <summary>
    /// Downloads a file from the server to local filesystem.
    /// </summary>
    /// <param name="path">File path relative to server's working directory.</param>
    /// <param name="outputFilePath">Local path where the downloaded file will be saved.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// GetResult containing download status, file information, or error details.
    /// If output file already exists, it will be overwritten.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when client is not connected.</exception>
    /// <remarks>
    /// Request format: "2 {path}"
    /// Response format: "size" followed by binary file content
    /// Special response: "-1" when file doesn't exist.
    /// </remarks>
    public async Task<GetResult> GetFileAsync(string path, string outputFilePath, CancellationToken cancellationToken = default)
    {
        if (this.stream == null || this.reader == null || this.writer == null)
        {
            throw new InvalidOperationException("Client not connected");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return new GetResult { Success = false, ErrorMessage = $"{BadRequestCode} Bad Request: Path cannot be empty" };
        }

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return new GetResult { Success = false, ErrorMessage = $"{BadRequestCode} Bad Request: Invalid path characters" };
        }

        await this.writer.WriteLineAsync($"2 {path}");
        await this.writer.FlushAsync(cancellationToken);

        var sizeLine = await this.reader.ReadLineAsync(cancellationToken);
        switch (sizeLine)
        {
            case null:
                return new GetResult { Success = false, ErrorMessage = "No response from server" };
            case DirectoryNotFoundResponse:
                return new GetResult { Success = true, FileExists = false };
        }

        if (sizeLine.StartsWith(BadRequestCode) || sizeLine.StartsWith(ForbiddenCode) || sizeLine.StartsWith(InternalServerErrorCode))
        {
            return new GetResult { Success = false, ErrorMessage = sizeLine };
        }

        if (!long.TryParse(sizeLine, out var fileSize) || fileSize < 0)
        {
            return new GetResult { Success = false, ErrorMessage = "Invalid file size in response" };
        }

        try
        {
            await using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            var buffer = new byte[8192];
            long totalBytesRead = 0;

            while (totalBytesRead < fileSize)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, fileSize - totalBytesRead);
                var bytesRead = await this.stream.ReadAsync(buffer.AsMemory(0, bytesToRead));

                if (bytesRead == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;
            }

            return new GetResult
            {
                Success = true,
                FileExists = true,
                FileSize = fileSize,
                DownloadedSize = totalBytesRead,
                OutputPath = outputFilePath,
            };
        }
        catch (Exception ex)
        {
            return new GetResult { Success = false, ErrorMessage = $"Error downloading file: {ex.Message}" };
        }
    }

    /// <summary>
    /// Releases all resources used by the Client.
    /// </summary>
    public void Dispose()
    {
        this.reader?.Dispose();
        this.writer?.Dispose();
        this.stream?.Dispose();
        this.client?.Dispose();
    }

    private static ListResult ParseListResponse(string response)
    {
        if (response.StartsWith(BadRequestCode) || response.StartsWith(ForbiddenCode) || response.StartsWith(InternalServerErrorCode))
        {
            return new ListResult { Success = false, ErrorMessage = response };
        }

        if (response == DirectoryNotFoundResponse)
        {
            return new ListResult { Success = true, Items = [], DirectoryExists = false };
        }

        try
        {
            var parts = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return new ListResult { Success = false, ErrorMessage = "Invalid response format: empty response" };
            }

            if (!int.TryParse(parts[0], out _))
            {
                return new ListResult { Success = false, ErrorMessage = "Invalid the number of files and folders in the directory in response" };
            }

            if ((parts.Length - 1) % 2 != 0)
            {
                return new ListResult { Success = false, ErrorMessage = "Invalid response format: mismatched name/isDir pairs" };
            }

            var items = new List<FileSystemItem>();
            for (var i = 1; i < parts.Length; i += 2)
            {
                if (i + 1 >= parts.Length)
                {
                    break;
                }

                var name = parts[i];
                var isDirStr = parts[i + 1];
                if (isDirStr != "true" && isDirStr != "false")
                {
                    return new ListResult { Success = false, ErrorMessage = $"Invalid response format: invalid isDir value '{isDirStr}'" };
                }

                var isDir = isDirStr == "true";

                items.Add(new FileSystemItem { Name = name, IsDirectory = isDir });
            }

            return new ListResult
            {
                Success = true,
                Items = items,
                DirectoryExists = true,
                Count = items.Count,
            };
        }
        catch (Exception ex)
        {
            return new ListResult { Success = false, ErrorMessage = $"Failed to parse response: {ex.Message}" };
        }
    }

    /// <summary>
    /// Represents the result of a file download operation.
    /// </summary>
    public class GetResult
    {
        /// <summary>
        /// Gets a value indicating whether indicates whether the operation completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets error message if the operation failed, null otherwise.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets a value indicating whether indicates whether the requested file exists on the server.
        /// </summary>
        public bool FileExists { get; init; }

        /// <summary>
        /// Gets size of the file in bytes as reported by the server.
        /// </summary>
        public long FileSize { get; init; }

        /// <summary>
        /// Gets actual number of bytes downloaded and written to disk.
        /// </summary>
        public long DownloadedSize { get; init; }

        /// <summary>
        /// Gets local file path where the downloaded content was saved.
        /// Null if file doesn't exist or download failed.
        /// </summary>
        public string? OutputPath { get; init; }
    }

    /// <summary>
    /// Represents the result of a directory listing operation.
    /// </summary>
    public class ListResult
    {
        /// <summary>
        /// Gets a value indicating whether indicates whether the operation completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets error message if the operation failed, null otherwise.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Gets a value indicating whether indicates whether the requested directory exists on the server.
        /// </summary>
        public bool DirectoryExists { get; init; }

        /// <summary>
        /// Gets number of items in the directory (files and subdirectories).
        /// </summary>
        public int Count { get; init; }

        /// <summary>
        /// Gets collection of files and subdirectories in the requested directory.
        /// Empty if directory doesn't exist or operation failed.
        /// </summary>
        public List<FileSystemItem>? Items { get; init; }
    }

    /// <summary>
    /// Represents a single filesystem entry (file or directory).
    /// </summary>
    public class FileSystemItem
    {
        /// <summary>
        /// Gets name of the file or directory.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether indicates whether this entry is a directory (true) or file (false).
        /// </summary>
        public bool IsDirectory { get; init; }
    }
}