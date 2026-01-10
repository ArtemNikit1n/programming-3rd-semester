// <copyright file="ServerListTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Server.Tests;

using System.Net;
using System.Net.Sockets;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ServerListTests
{
    private string testDirectory = string.Empty;
    private string testSubDirectory = string.Empty;
    private Server server;
    private int freePort;

    [SetUp]
    public void SetUp()
    {
        this.testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ServerTest_" + Guid.NewGuid().ToString("N"));
        this.testSubDirectory = Path.Combine(this.testDirectory, "SubDirectory");

        Directory.CreateDirectory(this.testDirectory);
        Directory.CreateDirectory(this.testSubDirectory);

        File.WriteAllText(Path.Combine(this.testDirectory, "file1.txt"), "content");
        File.WriteAllText(Path.Combine(this.testDirectory, "file2.txt"), "content");
        File.WriteAllText(Path.Combine(this.testSubDirectory, "nested.txt"), "content");

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
    public async Task ProcessRequest_Should_ReturnBadRequest_When_RequestIsEmpty()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync(string.Empty);

        Assert.That(response, Does.Contain("400"));
        Assert.That(response, Does.Contain("Empty request"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnBadRequest_When_RequestHasInvalidFormat()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync("1");

        Assert.That(response, Does.Contain("400"));
        Assert.That(response, Does.Contain("Invalid request format"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnBadRequest_When_PathContainsInvalidCharacters()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync("1 *");

        Assert.That(response, Does.Contain("-1"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnBadRequest_When_CommandIsUnknown()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync("unknown ./path");

        Assert.That(response, Does.Contain("400"));
        Assert.That(response, Does.Contain("Unknown command"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnBadRequest_When_PathIsEmpty()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync("1 ");

        Assert.That(response, Does.Contain("400"));
        Assert.That(response, Does.Contain("Path cannot be empty"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnForbidden_When_PathIsOutsideCurrentDirectory()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync("1 ../");

        Assert.That(response, Does.Contain("403"));
        Assert.That(response, Does.Contain("Access denied"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnDirectoryNotFound_When_DirectoryDoesNotExist()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync("1 ./nonexistent_directory");

        Assert.That(response, Is.EqualTo("-1"));
    }

    [Test]
    public async Task ProcessRequest_Should_ReturnDirectoryContents_When_DirectoryExists()
    {
        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync($"1 {this.testDirectory}");

        Assert.That(response, Does.StartWith("3"));
        Assert.That(response, Does.Contain("file1.txt false"));
        Assert.That(response, Does.Contain("file2.txt false"));
        Assert.That(response, Does.Contain("SubDirectory true"));
    }

    [Test]
    public async Task ProcessRequest_Should_SkipFilesWithSpacesInNames()
    {
        var fileWithSpace = Path.Combine(this.testDirectory, "file with space.txt");
        await File.WriteAllTextAsync(fileWithSpace, "content");

        _ = this.server.RunAsync();
        var response = await this.SendCommandAsync($"1 {this.testDirectory}");

        Assert.That(response, Does.Not.Contain("file with space.txt"));
    }

    [Test]
    public async Task IntegrationTest_Should_WorkWithRealClientServerCommunication()
    {
        _ = this.server.RunAsync();

        var client = Client.Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var result = await client.ListDirectoryAsync(this.testDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DirectoryExists, Is.True);
            Assert.That(result.Items, Has.Count.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public async Task ProcessRequest_Should_HandleMultipleClientsSimultaneously()
    {
        _ = this.server.RunAsync();

        const int clientCount = 5;
        var tasks = new List<Task<string>>();

        for (var i = 0; i < clientCount; i++)
        {
            tasks.Add(Task.Run(async () => await this.SendCommandAsync($"1 {this.testDirectory}"))!);
        }

        var responses = await Task.WhenAll(tasks);

        Assert.That(responses, Has.Length.EqualTo(clientCount));
        foreach (var response in responses)
        {
            Assert.That(response, Does.StartWith("3"));
            Assert.That(response, Does.Contain("file1.txt false"));
            Assert.That(response, Does.Contain("file2.txt false"));
            Assert.That(response, Does.Contain("SubDirectory true"));
        }
    }

    [Test]
    public async Task ProcessRequest_Should_HandleMixedCommandsFromMultipleClients()
    {
        _ = this.server.RunAsync();

        var tasks = new List<Task<string>>
        {
            this.SendCommandAsync("1 " + this.testDirectory)!,
            this.SendCommandAsync("1 ../")!,
            this.SendCommandAsync("1 ./nonexistent")!,
            this.SendCommandAsync("invalid command")!,
            this.SendCommandAsync(string.Empty)!,
        };

        var responses = await Task.WhenAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(responses[0], Does.StartWith("3"));
            Assert.That(responses[1], Does.Contain("403"));
            Assert.That(responses[2], Is.EqualTo("-1"));
            Assert.That(responses[3], Does.Contain("400"));
            Assert.That(responses[4], Does.Contain("400"));
        });
    }

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private async Task<string?> SendCommandAsync(string command)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, this.freePort);
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream);
        await using var writer = new StreamWriter(stream);

        await writer.WriteLineAsync(command);
        await writer.FlushAsync();
        return await reader.ReadLineAsync();
    }
}