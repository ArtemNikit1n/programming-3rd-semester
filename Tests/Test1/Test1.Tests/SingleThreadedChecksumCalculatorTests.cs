// <copyright file="SingleThreadedChecksumCalculatorTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Test1.Tests;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class SingleThreadedChecksumCalculatorTests
{
    private string testDirectory;

    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_ReturnConsistentHashForSameFile()
    {
        var testFile = Path.Combine(testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello, World!");

        var checksum1 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testFile);
        var checksum2 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testFile);

        Assert.That(checksum1, Is.EqualTo(checksum2));
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_ReturnConsistentHashForDirectory()
    {
        var subDir = Path.Combine(testDirectory, "SubDir");
        var file1 = Path.Combine(testDirectory, "file1.txt");
        var file2 = Path.Combine(subDir, "file2.txt");

        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(file1, "Content1");
        await File.WriteAllTextAsync(file2, "Content2");

        var checksum1 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDirectory);
        var checksum2 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDirectory);

        Assert.That(checksum1, Is.EqualTo(checksum2));
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_ReturnDifferentHashesForDifferentContent()
    {
        var testDir1 = Path.Combine(testDirectory, "Dir1");
        var testDir2 = Path.Combine(testDirectory, "Dir2");
        var file1 = Path.Combine(testDir1, "file.txt");
        var file2 = Path.Combine(testDir2, "file.txt");

        Directory.CreateDirectory(testDir1);
        Directory.CreateDirectory(testDir2);
        await File.WriteAllTextAsync(file1, "Content1");
        await File.WriteAllTextAsync(file2, "Content2");

        var checksum1 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDir1);
        var checksum2 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDir2);

        Assert.That(checksum1, Is.Not.EqualTo(checksum2));
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_BeOrderIndependent()
    {
        var testDir1 = Path.Combine(testDirectory, "SameNameDir");
        var testDir2 = Path.Combine(testDirectory, "SameNameDir");

        Directory.CreateDirectory(testDir1);
        Directory.CreateDirectory(testDir2);

        var file1a = Path.Combine(testDir1, "a.txt");
        var file1b = Path.Combine(testDir1, "b.txt");
        var file2b = Path.Combine(testDir2, "b.txt");
        var file2a = Path.Combine(testDir2, "a.txt");

        await File.WriteAllTextAsync(file1a, "ContentA");
        await File.WriteAllTextAsync(file1b, "ContentB");
        await File.WriteAllTextAsync(file2a, "ContentA");
        await File.WriteAllTextAsync(file2b, "ContentB");

        var checksum1 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDir1);
        var checksum2 = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDir2);

        Assert.That(checksum1, Is.EqualTo(checksum2));
    }

    [Test]
    public void ComputeDirectoryChecksumAsync_Should_ThrowDirectoryNotFoundExceptionForInvalidPath()
    {
        var invalidPath = Path.Combine(testDirectory, "NonExistentDirectory");

        Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(invalidPath));
    }

    [Test]
    public void ComputeDirectoryChecksumAsync_Should_ThrowArgumentNullExceptionForNullPath()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(null!));
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_CancelOperationWhenCancellationRequested()
    {
        var largeFile = Path.Combine(testDirectory, "large.bin");
        await using (var fileStream = File.Create(largeFile))
        {
            fileStream.SetLength(10 * 1024 * 1024);
        }

        using var cts = new CancellationTokenSource();

        var task = SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDirectory, cts.Token);
        await cts.CancelAsync();

        Assert.ThrowsAsync<OperationCanceledException>(() => task);
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_HandleLargeFilesWithBuffer()
    {
        var largeFile = Path.Combine(testDirectory, "large.txt");
        await using (var writer = File.CreateText(largeFile))
        {
            for (var i = 0; i < 100000; i++)
            {
                await writer.WriteLineAsync($"Line {i}: This is test content for large file handling");
            }
        }

        var checksum = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(largeFile);

        Assert.That(checksum, Is.Not.Null);
        Assert.That(checksum, Has.Length.EqualTo(16));
    }

    [Test]
    public async Task ComputeDirectoryChecksumAsync_Should_ComputeCorrectChecksumForEmptyDirectory()
    {
        var checksum = await SingleThreadedChecksumCalculator.ComputeDirectoryChecksumAsync(testDirectory);

        Assert.That(checksum, Is.Not.Null);
        Assert.That(checksum.Length, Is.EqualTo(16));
    }
}