// <copyright file="Server.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Server;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// TCP server that provides file listing and file download services.
/// Supports multiple concurrent client connections.
/// Implements IDisposable for proper resource cleanup.
/// </summary>
/// <remarks>
/// The server uses a simple text-based protocol:
/// - Directory listing: "1 {path}" → "size name1 isDir1 name2 isDir2 ..."
/// - File download: "2 {path}" → "size" + binary content
/// Security: Restricts file access to server's working directory and subdirectories.
/// </remarks>
/// <param name="port">Port number to listen on (default: 8080).</param>
public class Server(int port = 8080) : IDisposable
{
    private const string DirectoryNotFoundResponse = "-1";
    private const string BadRequestCode = "400";
    private const string ForbiddenCode = "403";
    private const string InternalServerErrorCode = "500";
    private readonly Lazy<TcpListener> listener = new(() => new TcpListener(IPAddress.Any, port));
    private readonly CancellationTokenSource serverCts = new();
    private readonly List<Task> currentTasks = [];

    /// <summary>
    /// Starts the server and begins accepting client connections.
    /// </summary>
    /// <remarks>
    /// The server runs until Dispose() is called.
    /// Each client connection is processed in a separate task.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when server is disposed.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAsync()
    {
        ObjectDisposedException.ThrowIf(this.serverCts.Token.IsCancellationRequested, nameof(Server));

        var listenerInstance = this.listener.Value;
        listenerInstance.Start();
        while (!this.serverCts.Token.IsCancellationRequested)
        {
            try
            {
                var socket = await listenerInstance.AcceptSocketAsync(this.serverCts.Token);
                var task = Task.Run(async () =>
                {
                    await using var stream = new NetworkStream(socket);
                    using var reader = new StreamReader(stream);
                    var data = await reader.ReadLineAsync();
                    if (data is not null)
                    {
                        var response = await ProcessRequestAsync(data, stream);
                        if (response.IsTextResponse)
                        {
                            await using var writer = new StreamWriter(stream);
                            await writer.WriteLineAsync(response.Response);
                            await writer.FlushAsync();
                        }
                    }

                    socket.Close();
                });
                this.currentTasks.Add(task);
                this.currentTasks.RemoveAll(t => t.IsCompleted);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Stops the server and releases all resources.
    /// </summary>
    /// <remarks>
    /// Waits for ongoing client operations to complete before shutting down.
    /// </remarks>
    public void Dispose()
    {
        if (this.serverCts.Token.IsCancellationRequested || !this.listener.IsValueCreated)
        {
            return;
        }

        this.listener.Value.Stop();
        this.serverCts.Cancel();
        try
        {
            Task.WaitAll(this.currentTasks.ToArray());
        }
        catch (OperationCanceledException)
        {
        }

        this.listener.Value.Dispose();
        this.serverCts.Dispose();
    }

    private static async Task<(string Response, bool IsTextResponse)> ProcessRequestAsync(string request, NetworkStream stream)
    {
        if (string.IsNullOrEmpty(request))
        {
            return ($"{BadRequestCode} Bad Request: Empty request", true);
        }

        var commandAndPath = request.Split(' ');
        if (commandAndPath.Length < 2)
        {
            return ($"{BadRequestCode} Bad Request: Invalid request format", true);
        }

        var command = commandAndPath[0];
        var path = commandAndPath[1];

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return ($"{BadRequestCode} Bad Request: Invalid path characters", true);
        }

        return command switch
        {
            "1" => (await HandleListCommandAsync(path), true),
            "2" => await HandleGetCommandAsync(path, stream),

            // ReSharper disable once ArrangeTrailingCommaInMultilineLists
            _ => ($"{BadRequestCode} Bad Request: Unknown command", true),
        };
    }

    private static Task<string> HandleListCommandAsync(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                return Task.FromResult($"{BadRequestCode} Bad Request: Path cannot be empty");
            }

            var fullPath = Path.GetFullPath(path);
            var currentDirectory = Directory.GetCurrentDirectory();

            if (!fullPath.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult($"{ForbiddenCode} Forbidden: Access denied");
            }

            if (!Directory.Exists(fullPath))
            {
                return Task.FromResult(DirectoryNotFoundResponse);
            }

            var entries = Directory.GetFileSystemEntries(fullPath);
            var result = new System.Text.StringBuilder();
            result.Append(entries.Length);
            foreach (var entry in entries)
            {
                var name = Path.GetFileName(entry);
                var isDirectory = Directory.Exists(entry);
                if (string.IsNullOrEmpty(name) || name.Contains(' '))
                {
                    continue;
                }

                result.Append($" {name} {(isDirectory ? "true" : "false")} ");
            }

            result.Append('\n');
            return Task.FromResult(result.ToString());
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult($"{ForbiddenCode} Forbidden: Access denied");
        }
        catch (PathTooLongException)
        {
            return Task.FromResult($"{BadRequestCode} Bad Request: Path too long");
        }
        catch (Exception ex) when (ex is IOException or ArgumentException)
        {
            return Task.FromResult($"{BadRequestCode} Bad Request: Invalid path");
        }
        catch (Exception)
        {
            return Task.FromResult($"{InternalServerErrorCode} Internal Server Error: Please, try again later");
        }
    }

    private static async Task<(string Response, bool IsTextResponse)> HandleGetCommandAsync(string path, NetworkStream stream)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                return ($"{BadRequestCode} Bad Request: Path cannot be empty", true);
            }

            var fullPath = Path.GetFullPath(path);
            var currentDirectory = Directory.GetCurrentDirectory();

            if (!fullPath.StartsWith(currentDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return ($"{ForbiddenCode} Forbidden: Access denied", true);
            }

            if (Directory.Exists(fullPath))
            {
                return ($"{BadRequestCode} Bad Request: Path is a directory, not a file", true);
            }

            if (!File.Exists(fullPath))
            {
                return ("-1", true);
            }

            var fileInfo = new FileInfo(fullPath);
            var fileSize = fileInfo.Length;

            var sizeHeader = $"{fileSize}\n";
            var headerBytes = System.Text.Encoding.UTF8.GetBytes(sizeHeader);
            await stream.WriteAsync(headerBytes);

            await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            return (string.Empty, false);
        }
        catch (UnauthorizedAccessException)
        {
            return ($"{ForbiddenCode} Forbidden: Access denied", true);
        }
        catch (PathTooLongException)
        {
            return ($"{BadRequestCode} Bad Request: Path too long", true);
        }
        catch (IOException ioEx)
        {
            return ($"{BadRequestCode} Bad Request: IO error - {ioEx.Message}", true);
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            return ($"{BadRequestCode} Bad Request: Invalid path", true);
        }
        catch (Exception)
        {
            return ($"{InternalServerErrorCode} Internal Server Error: Please, try again later", true);
        }
    }
}