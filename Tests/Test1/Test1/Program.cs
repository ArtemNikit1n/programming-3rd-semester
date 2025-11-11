// <copyright file="Program.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Test1;

if (args.Length != 1)
{
    Console.WriteLine("Usage: DirectoryChecksum <directory_path>");
    return;
}

var directoryPath = args[0];

if (!Directory.Exists(directoryPath))
{
    Console.WriteLine($"Directory not found: {directoryPath}");
    return;
}

try
{
    Console.WriteLine($"Computing checksum for directory: {directoryPath}");

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();

        Console.WriteLine("Cancellation requested...");
    };

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var checksum = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(directoryPath, cts.Token);
    stopwatch.Stop();

    var checksumString = Convert.ToHexStringLower(checksum);

    Console.WriteLine($"Checksum: {checksumString}");
    Console.WriteLine($"Time taken: {stopwatch.Elapsed}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}