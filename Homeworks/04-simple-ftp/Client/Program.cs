// <copyright file="Program.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

Client.Client? client;

var host = "localhost";
var port = 8080;

if (args.Length > 0)
{
    host = args[0];
    if (args.Length > 1 && int.TryParse(args[1], out var customPort))
    {
        if (customPort is < 1024 or > 49151)
        {
            Console.WriteLine("Error: Port must be between 1024 and 49151.");
            return;
        }

        port = customPort;
    }
}

try
{
    Console.WriteLine($"Connecting to {host}:{port}...");
    client = await Client.Client.CreateAndConnectAsync(host, port);
    Console.WriteLine("Connected successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to connect: {ex.Message}");
    return;
}

var localClient = client;

try
{
    await RunInteractiveMode(localClient);
}
finally
{
    client.Dispose();
}

return;

async Task RunInteractiveMode(Client.Client clientInstance)
{
    Console.WriteLine("\nAvailable commands:");
    Console.WriteLine("  list <path>             - List directory contents");
    Console.WriteLine("  get <remote> <local>    - Download file from server");
    Console.WriteLine("  exit                    - Exit the application");
    Console.WriteLine();

    while (true)
    {
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            continue;
        }

        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Goodbye!");
            break;
        }

        await ProcessCommand(input, clientInstance);
    }
}

async Task ProcessCommand(string command, Client.Client clientInstance)
{
    var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    try
    {
        switch (parts[0].ToLower())
        {
            case "list":
                await HandleListCommandAsync(parts, clientInstance);
                break;

            case "get":
                await HandleGetCommandAsync(parts, clientInstance);
                break;

            default:
                Console.WriteLine($"Unknown command: {parts[0]}");
                Console.WriteLine("Available commands: list, get, exit");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

async Task HandleListCommandAsync(string[] parts, Client.Client clientInstance)
{
    ArgumentNullException.ThrowIfNull(clientInstance);

    if (parts.Length < 2)
    {
        Console.WriteLine("Usage: list <path>");
        return;
    }

    var path = parts[1];
    Console.WriteLine($"Requesting directory listing for: {path}");

    var result = await clientInstance.ListDirectoryAsync(path);

    if (!result.Success)
    {
        Console.WriteLine($"Error: {result.ErrorMessage}");
        return;
    }

    if (!result.DirectoryExists)
    {
        Console.WriteLine("Directory does not exist on server.");
        return;
    }

    Console.WriteLine($"Directory contains {result.Count} items:");
    Console.WriteLine();

    if (result.Items is { Count: > 0 })
    {
        Console.WriteLine($"{"Type",-10} Name");
        Console.WriteLine(new string('-', 50));

        foreach (var item in result.Items)
        {
            var type = item.IsDirectory ? "[DIR]" : "[FILE]";
            Console.WriteLine($"{type,-10} {item.Name}");
        }
    }
    else
    {
        Console.WriteLine("(Empty directory)");
    }
}

async Task HandleGetCommandAsync(string[] parts, Client.Client clientInstance)
{
    ArgumentNullException.ThrowIfNull(clientInstance);

    if (parts.Length < 3)
    {
        Console.WriteLine("Usage: get <remote_path> <local_path>");
        return;
    }

    var remotePath = parts[1];
    var localPath = parts[2];

    Console.WriteLine($"Downloading '{remotePath}' to '{localPath}'...");

    var result = await clientInstance.GetFileAsync(remotePath, localPath);

    if (!result.Success)
    {
        Console.WriteLine($"Error: {result.ErrorMessage}");
        return;
    }

    if (!result.FileExists)
    {
        Console.WriteLine("File does not exist on server.");
        return;
    }

    Console.WriteLine("Download completed successfully!");
    Console.WriteLine($"  File size: {result.FileSize} bytes");
    Console.WriteLine($"  Downloaded: {result.DownloadedSize} bytes");
    Console.WriteLine($"  Saved to: {result.OutputPath}");
}