// <copyright file="ClientListTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Client.Tests;

using System.Net;
using System.Net.Sockets;

#pragma warning disable SA1600
public class ClientListTests
{
    private Server.Server server;
    private int freePort;
    private string testDirectory = string.Empty;
    private string testSubDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this.testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ClientListTest_" + Guid.NewGuid().ToString("N"));
        this.testSubDirectory = Path.Combine(this.testDirectory, "SubDirectory");

        Directory.CreateDirectory(this.testDirectory);
        Directory.CreateDirectory(this.testSubDirectory);

        File.WriteAllText(Path.Combine(this.testDirectory, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(this.testDirectory, "file2.txt"), "content2");
        File.WriteAllText(Path.Combine(this.testSubDirectory, "nested.txt"), "nested content");

        this.freePort = FindFreePort();
        this.server = new Server.Server(this.freePort);
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
    public async Task ListDirectoryAsync_Should_ReturnBadRequest_When_PathIsEmpty()
    {
        _ = this.server.RunAsync();

        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;
        var result = await client.ListDirectoryAsync(string.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("400"));
            Assert.That(result.ErrorMessage, Does.Contain("Path cannot be empty"));
        });
    }

    [Test]
    public async Task ListDirectoryAsync_Should_ReturnBadRequest_When_PathContainsInvalidCharacters()
    {
        _ = this.server.RunAsync();

        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var result = await client.ListDirectoryAsync("invalid|path*");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("400"));
            Assert.That(result.ErrorMessage, Does.Contain("Invalid path characters"));
        });
    }

    [Test]
    public async Task ListDirectoryAsync_Should_ReturnDirectoryNotFound_When_DirectoryDoesNotExist()
    {
        _ = this.server.RunAsync();

        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var result = await client.ListDirectoryAsync("./nonexistent_directory");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DirectoryExists, Is.False);
            Assert.That(result.Items, Is.Empty);
        });
    }

    [Test]
    public async Task ListDirectoryAsync_Should_ReturnDirectoryContents_When_DirectoryExists()
    {
        _ = this.server.RunAsync();

        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var result = await client.ListDirectoryAsync(this.testDirectory);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DirectoryExists, Is.True);
            Assert.That(result.Items, Has.Count.EqualTo(3));

            var file1 = result.Items!.FirstOrDefault(i => i.Name == "file1.txt");
            Assert.That(file1, Is.Not.Null);
            Assert.That(file1 is { IsDirectory: true }, Is.False);

            var file2 = result.Items!.FirstOrDefault(i => i.Name == "file2.txt");
            Assert.That(file2, Is.Not.Null);
            Assert.That(file2!.IsDirectory, Is.False);

            var subDir = result.Items!.FirstOrDefault(i => i.Name == "SubDirectory");
            Assert.That(subDir, Is.Not.Null);
            Assert.That(subDir is { IsDirectory: true }, Is.True);
        });
    }

    [Test]
    public async Task ListDirectoryAsync_Should_ReturnForbidden_When_PathIsOutsideCurrentDirectory()
    {
        _ = this.server.RunAsync();

        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var result = await client.ListDirectoryAsync("../../");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("403"));
            Assert.That(result.ErrorMessage, Does.Contain("Access denied"));
        });
    }

    [Test]
    public async Task ListDirectoryAsync_Should_SkipFilesWithSpacesInNames()
    {
        var fileWithSpace = Path.Combine(this.testDirectory, "file with space.txt");
        await File.WriteAllTextAsync(fileWithSpace, "content");

        _ = this.server.RunAsync();

        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var result = await client.ListDirectoryAsync(this.testDirectory);

        Assert.That(result.Items!.Any(i => i.Name.Contains(' ')), Is.False);
    }

    [Test]
    public async Task ListDirectoryAsync_Should_WorkWithRelativePath_When_DirectoryInCurrentDirectory()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), this.testDirectory);

        var result = await client.ListDirectoryAsync($"./{relativePath}");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DirectoryExists, Is.True);
            Assert.That(result.Items, Has.Count.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public async Task ListDirectoryAsync_Should_HandleMultipleClientsSimultaneously()
    {
        _ = this.server.RunAsync();

        var clientTasks = new List<Task<Client.ListResult>>();

        for (var i = 0; i < 3; i++)
        {
            var clientTask = Task.Run(async () =>
            {
                try
                {
                    var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

                    return await client.ListDirectoryAsync(this.testDirectory);
                }
                catch
                {
                    return new Client.ListResult { Success = false };
                }
            });

            clientTasks.Add(clientTask);
        }

        var completedResults = await Task.WhenAll(clientTasks);

        Assert.That(completedResults, Has.Length.EqualTo(3));
        Assert.That(completedResults.All(r => r.Success && r.DirectoryExists), Is.True);
    }

    [Test]
    public async Task ListDirectoryAsync_Should_ReturnEmptyList_When_DirectoryIsEmpty()
    {
        var emptyDirectory = Path.Combine(Directory.GetCurrentDirectory(), "EmptyTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyDirectory);

        try
        {
            _ = this.server.RunAsync();

            var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

            var result = await client.ListDirectoryAsync(emptyDirectory);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.DirectoryExists, Is.True);
                Assert.That(result.Items, Is.Empty);
                Assert.That(result.Count, Is.EqualTo(0));
            });
        }
        finally
        {
            if (Directory.Exists(emptyDirectory))
            {
                Directory.Delete(emptyDirectory);
            }
        }
    }

    [Test]
    public void ListDirectoryAsync_Should_ThrowInvalidOperationException_When_ClientNotConnected()
    {
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        Assert.ThrowsAsync<InvalidOperationException>(() => client.ListDirectoryAsync("./test"));
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