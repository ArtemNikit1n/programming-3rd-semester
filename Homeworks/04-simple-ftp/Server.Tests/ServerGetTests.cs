// <copyright file="ServerGetTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Server.Tests;

using System.Net;
using System.Net.Sockets;
using System.Text;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ServerGetTests
{
    private const string TestFileContent = "Hello! This is test file content.";
    private Server server;
    private int freePort;
    private string testDirectory = string.Empty;
    private string testFile = string.Empty;
    private string largeTestFile = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this.testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ServerGetTest_" + Guid.NewGuid().ToString("N"));
        this.testFile = Path.Combine(this.testDirectory, "testfile.txt");
        this.largeTestFile = Path.Combine(this.testDirectory, "largefile.bin");

        Directory.CreateDirectory(this.testDirectory);
        File.WriteAllText(this.testFile, TestFileContent);

        var largeContent = new string('X', 100000);
        File.WriteAllText(this.largeTestFile, largeContent);

        this.freePort = FindFreePort();
        this.server = new Server(this.freePort);
    }

    [TearDown]
    public void TearDown()
    {
        this.server.Dispose();
        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, true);
        }
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnBadRequest_When_GetPathIsEmpty()
    {
        _ = this.server.RunAsync();

        using var client = new TcpClient("localhost", this.freePort);
        var stream = client.GetStream();
        var writer = new StreamWriter(stream);
        var reader = new StreamReader(stream);

        await writer.WriteLineAsync("2 ");
        await writer.FlushAsync();
        var response = await reader.ReadLineAsync();

        Assert.That(response, Does.Contain("400"));
        Assert.That(response, Does.Contain("Path cannot be empty"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnForbidden_When_GetPathIsOutsideCurrentDirectory()
    {
        _ = this.server.RunAsync();

        using var client = new TcpClient("localhost", this.freePort);
        var stream = client.GetStream();
        var writer = new StreamWriter(stream);
        var reader = new StreamReader(stream);

        await writer.WriteLineAsync("2 ../../sensitive.txt");
        await writer.FlushAsync();
        var response = await reader.ReadLineAsync();

        Assert.That(response, Does.Contain("403"));
        Assert.That(response, Does.Contain("Access denied"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnFileNotFound_When_FileDoesNotExist()
    {
        _ = this.server.RunAsync();

        using var client = new TcpClient("localhost", this.freePort);
        var stream = client.GetStream();
        var writer = new StreamWriter(stream);
        var reader = new StreamReader(stream);

        await writer.WriteLineAsync("2 ./nonexistent.txt");
        await writer.FlushAsync();
        var response = await reader.ReadLineAsync();

        Assert.That(response, Is.EqualTo("-1"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnBadRequest_When_GetPathIsDirectory()
    {
        _ = this.server.RunAsync();

        using var client = new TcpClient("localhost", this.freePort);
        var stream = client.GetStream();
        var writer = new StreamWriter(stream);
        var reader = new StreamReader(stream);

        await writer.WriteLineAsync($"2 {this.testDirectory}");
        await writer.FlushAsync();
        var response = await reader.ReadLineAsync();

        Assert.That(response, Does.Contain("400"));
    }

    [Test]
    public async Task ProcessRequest_Should_SendFileContent_When_FileExists()
    {
        _ = this.server.RunAsync();

        using var client = new TcpClient("localhost", this.freePort);
        var stream = client.GetStream();
        var writer = new StreamWriter(stream);
        var reader = new StreamReader(stream);

        await writer.WriteLineAsync($"2 {this.testFile}");
        await writer.FlushAsync();

        var sizeLine = await reader.ReadLineAsync();
        Assert.That(long.TryParse(sizeLine, out var fileSize), Is.True);
        Assert.That(fileSize, Is.EqualTo(TestFileContent.Length));

        var buffer = new byte[fileSize];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)fileSize));
        var receivedContent = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Assert.That(receivedContent, Is.EqualTo(TestFileContent));
    }

    [Test]
    public async Task ProcessRequest_Should_SendLargeFileCorrectly_When_FileIsLarge()
    {
        _ = this.server.RunAsync();

        using var client = new TcpClient("localhost", this.freePort);
        var stream = client.GetStream();
        var writer = new StreamWriter(stream);
        var reader = new StreamReader(stream);

        await writer.WriteLineAsync($"2 {this.largeTestFile}");
        await writer.FlushAsync();

        var sizeLine = await reader.ReadLineAsync();
        Assert.That(long.TryParse(sizeLine, out var fileSize), Is.True);
        Assert.That(fileSize, Is.GreaterThan(80000));

        var totalBytesRead = 0;
        var buffer = new byte[8192];
        var receivedContent = new MemoryStream();

        while (totalBytesRead < fileSize)
        {
            var bytesToRead = Math.Min(buffer.Length, (int)(fileSize - totalBytesRead));
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead));

            if (bytesRead == 0)
            {
                break;
            }

            await receivedContent.WriteAsync(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
        }

        Assert.That(totalBytesRead, Is.EqualTo(fileSize));
        var content = Encoding.UTF8.GetString(receivedContent.ToArray());
        Assert.That(content, Has.Length.EqualTo((int)fileSize));
    }

    [Test]
    public async Task ProcessRequest_Should_HandleMultipleClientsSimultaneously_When_DownloadingFiles()
    {
        _ = this.server.RunAsync();

        var clientTasks = new List<Task>();
        var results = new List<bool>();

        for (var i = 0; i < 3; i++)
        {
            var clientTask = Task.Run(async () =>
            {
                try
                {
                    using var client = new TcpClient("localhost", this.freePort);
                    var stream = client.GetStream();
                    var writer = new StreamWriter(stream);
                    var reader = new StreamReader(stream);

                    await writer.WriteLineAsync($"2 {this.testFile}");
                    await writer.FlushAsync();

                    var sizeLine = await reader.ReadLineAsync();
                    if (long.TryParse(sizeLine, out var fileSize))
                    {
                        var buffer = new byte[fileSize];
                        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)fileSize));
                        var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        lock (results)
                        {
                            results.Add(content == TestFileContent);
                        }
                    }
                }
                catch
                {
                    lock (results)
                    {
                        results.Add(false);
                    }
                }
            });

            clientTasks.Add(clientTask);
        }

        await Task.WhenAll(clientTasks);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results.All(r => r), Is.True);
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}