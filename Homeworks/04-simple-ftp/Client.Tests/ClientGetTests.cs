// <copyright file="ClientGetTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Client.Tests;

using System.Net;

using System.Net.Sockets;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ClientGetTests
{
    private Server.Server server;
    private int freePort;
    private string testDirectory = string.Empty;
    private string testFile = string.Empty;
    private string largeTestFile = string.Empty;
    private string tempOutputDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this.testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ClientGetTest_" + Guid.NewGuid().ToString("N"));
        this.testFile = Path.Combine(this.testDirectory, "testfile.txt");
        this.largeTestFile = Path.Combine(this.testDirectory, "largefile.bin");
        this.tempOutputDir = Path.Combine(Directory.GetCurrentDirectory(), "ClientGetOutput_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.testDirectory);
        Directory.CreateDirectory(this.tempOutputDir);
        File.WriteAllText(this.testFile, "Hello! This is test file content for download.");
        var largeContent = new string('X', 100000);
        File.WriteAllText(this.largeTestFile, largeContent);
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

        if (Directory.Exists(this.tempOutputDir))
        {
            Directory.Delete(this.tempOutputDir, true);
        }
    }

    [Test]
    public void GetFileAsync_Should_ThrowInvalidOperationException_When_ClientNotConnected()
    {
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "output.txt");

        Assert.ThrowsAsync<InvalidOperationException>(() => client.GetFileAsync("./test.txt", outputPath));
    }

    [Test]
    public async Task GetFileAsync_Should_ReturnBadRequest_When_PathIsEmpty()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "output.txt");
        var result = await client.GetFileAsync(string.Empty, outputPath);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("400"));
            Assert.That(result.ErrorMessage, Does.Contain("Path cannot be empty"));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_ReturnBadRequest_When_PathContainsInvalidCharacters()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "output.txt");
        var result = await client.GetFileAsync("invalid|path*.txt", outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("400"));
            Assert.That(result.ErrorMessage, Does.Contain("Invalid path characters"));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_ReturnFileNotFound_When_FileDoesNotExist()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "output.txt");
        var result = await client.GetFileAsync($"./nonexistent_file_{Guid.NewGuid():N}.txt", outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileExists, Is.False);
            Assert.That(result.FileSize, Is.EqualTo(0));
            Assert.That(result.DownloadedSize, Is.EqualTo(0));
            Assert.That(File.Exists(outputPath), Is.False);
        });
    }

    [Test]
    public async Task GetFileAsync_Should_DownloadFileSuccessfully_When_FileExists()
    {
        var testContent = "Hello! This is test file content for download.";
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "downloaded.txt");
        var result = await client.GetFileAsync(this.testFile, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileExists, Is.True);
            Assert.That(result.FileSize, Is.EqualTo(testContent.Length));
            Assert.That(result.DownloadedSize, Is.EqualTo(testContent.Length));
            Assert.That(result.OutputPath, Is.EqualTo(outputPath));
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(File.ReadAllText(outputPath), Is.EqualTo(testContent));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_DownloadLargeFileCorrectly_When_FileIsLarge()
    {
        var largeContent = new string('X', 100000);
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "large_downloaded.bin");
        var result = await client.GetFileAsync(this.largeTestFile, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileExists, Is.True);
            Assert.That(result.FileSize, Is.EqualTo(largeContent.Length));
            Assert.That(result.DownloadedSize, Is.EqualTo(largeContent.Length));
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(File.ReadAllText(outputPath), Is.EqualTo(largeContent));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_ReturnForbidden_When_PathIsOutsideCurrentDirectory()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "output.txt");
        var result = await client.GetFileAsync("../../sensitive.txt", outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("403"));
            Assert.That(result.ErrorMessage, Does.Contain("Access denied"));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_ReturnBadRequest_When_PathIsDirectory()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "output.txt");
        var result = await client.GetFileAsync(this.testDirectory, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("400"));
            Assert.That(result.ErrorMessage, Does.Contain("Path is a directory, not a file"));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_WorkWithRelativePath_When_FileInCurrentDirectory()
    {
        var relativeTestFile = Path.GetRelativePath(Directory.GetCurrentDirectory(), this.testFile);
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "relative_downloaded.txt");
        var result = await client.GetFileAsync($"./{relativeTestFile}", outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileExists, Is.True);
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(File.ReadAllText(outputPath), Is.EqualTo(File.ReadAllText(this.testFile)));
        });
    }

    [Test]
    public async Task GetFileAsync_Should_DownloadZeroByteFile_When_FileIsEmpty()
    {
        var emptyFile = Path.Combine(this.testDirectory, "empty.txt");
        await File.WriteAllTextAsync(emptyFile, string.Empty);
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "empty_downloaded.txt");
        var result = await client.GetFileAsync(emptyFile, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileExists, Is.True);
            Assert.That(result.FileSize, Is.EqualTo(0));
            Assert.That(result.DownloadedSize, Is.EqualTo(0));
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(File.ReadAllText(outputPath), Is.Empty);
        });
    }

    [Test]
    public async Task GetFileAsync_Should_HandleMultipleClientsSimultaneously()
    {
        _ = this.server.RunAsync();
        var clientTasks = new List<Task<Client.GetResult>>();
        var outputFiles = new List<string>();

        for (var i = 0; i < 3; i++)
        {
            var outputPath = Path.Combine(this.tempOutputDir, $"parallel_{i}.txt");
            outputFiles.Add(outputPath);
            var clientTask = Task.Run(async () =>
            {
                try
                {
                    var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;
                    return await client.GetFileAsync(this.testFile, outputPath);
                }
                catch
                {
                    return new Client.GetResult { Success = false };
                }
            });

            clientTasks.Add(clientTask);
        }

        var completedResults = await Task.WhenAll(clientTasks);
        Assert.That(completedResults, Has.Length.EqualTo(3));
        Assert.That(completedResults.All(r => r.Success && r.FileExists), Is.True);

        foreach (var outputFile in outputFiles)
        {
            await Assert.MultipleAsync(async () =>
            {
                Assert.That(File.Exists(outputFile), Is.True);
                Assert.That(await File.ReadAllTextAsync(outputFile), Is.EqualTo(await File.ReadAllTextAsync(this.testFile)));
            });
        }
    }

    [Test]
    public async Task GetFileAsync_Should_OverwriteOutputFile_When_OutputFileAlreadyExists()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var outputPath = Path.Combine(this.tempOutputDir, "existing.txt");
        await File.WriteAllTextAsync(outputPath, "Old content that should be overwritten");
        var result = await client.GetFileAsync(this.testFile, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileExists, Is.True);
            Assert.That(File.ReadAllText(outputPath), Is.EqualTo(File.ReadAllText(this.testFile)));
            Assert.That(File.ReadAllText(outputPath), Is.Not.EqualTo("Old content that should be overwritten"));
        });
    }

    [Test]

    public async Task GetFileAsync_Should_ReturnError_When_OutputDirectoryDoesNotExist()
    {
        _ = this.server.RunAsync();
        var client = Client.CreateAndConnectAsync("localhost", this.freePort).Result;

        var nonExistentDir = Path.Combine(Directory.GetCurrentDirectory(), "NonExistentDir_" + Guid.NewGuid().ToString("N"));
        var outputPath = Path.Combine(nonExistentDir, "output.txt");
        var result = await client.GetFileAsync(this.testFile, outputPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Error downloading file"));
            Assert.That(File.Exists(outputPath), Is.False);
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
}