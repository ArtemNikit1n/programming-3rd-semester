// <copyright file="TestService.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnitWeb;

using System.Collections.Concurrent;
using MyNUnit.DataModels;

// ReSharper disable once RedundantNameQualifier
using MyNUnitWeb.ModelsWeb;

/// <summary>
/// A service for working with tests, providing functionality for downloading builds, running tests, and managing history.
/// </summary>
public class TestService
{
    private readonly ConcurrentDictionary<Guid, TestRun> testRuns = new();
    private readonly string uploadsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestService"/> class.
    /// </summary>
    /// <param name="environment">Web application environment, used to obtain the path to the root folder.</param>
    public TestService(IWebHostEnvironment environment)
    {
        this.uploadsPath = Path.Combine(environment.ContentRootPath, "Uploads");
        if (!Directory.Exists(this.uploadsPath))
        {
            Directory.CreateDirectory(this.uploadsPath);
        }
    }

    /// <summary>
    /// Uploads a DLL assembly to the server.
    /// </summary>
    /// <param name="file">The DLL file to upload.</param>
    /// <returns>The path to the saved file on the server.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the file is empty or not a DLL file.
    /// </exception>
    public async Task<string> UploadAssembly(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        if (!Path.GetExtension(file.FileName).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only .dll files are allowed");
        }

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(this.uploadsPath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filePath;
    }

    /// <summary>
    /// Runs tests for the specified assemblies.
    /// </summary>
    /// <param name="assemblyPaths">List of paths to assemblies to test.</param>
    /// <returns>Result of running tests.</returns>
    public async Task<TestRun> RunTests(List<string> assemblyPaths)
    {
        var testRun = new TestRun();
        var tasks = assemblyPaths.Select(assemblyPath => ProcessAssemblyAsync(assemblyPath, testRun)).ToList();

        await Task.WhenAll(tasks);
        this.testRuns.TryAdd(testRun.Id, testRun);
        return testRun;
    }

    /// <summary>
    /// Gets the test run history.
    /// </summary>
    /// <returns>A list of all completed test runs, sorted by time (newest first).</returns>
    public List<TestRun> GetTestRunHistory()
        => this.testRuns.Values.OrderByDescending(r => r.RunTime).ToList();

    /// <summary>
    /// Gets a specific test run by its ID.
    /// </summary>
    /// <param name="id">Unique test run ID.</param>
    /// <returns>The test run, or null if not found.</returns>
    public TestRun? GetTestRun(Guid id)
        => this.testRuns.GetValueOrDefault(id);

    /// <summary>
    /// Clears the test run history.
    /// </summary>
    public void ClearHistory()
        => this.testRuns.Clear();

    private static async Task ProcessAssemblyAsync(string assemblyPath, TestRun testRun)
    {
        if (!File.Exists(assemblyPath))
        {
            return;
        }

        var assemblyResults = new AssemblyTestResult
        {
            AssemblyName = Path.GetFileName(assemblyPath),
            AssemblyPath = assemblyPath,
        };

        try
        {
            var results = await MyNUnit.Core.TestExecutor.ExecuteTestsAsync([assemblyPath]);

            foreach (var result in results)
            {
                assemblyResults.TestResults.Add(new ModelsWeb.TestResult
                {
                    TestClass = result.TestClass,
                    TestMethod = result.TestMethod,
                    Status = result.Status,
                    DurationMs = result.Duration.TotalMilliseconds,
                    Messages = result.Messages.ToList(),
                    ExceptionType = result.Exception?.GetType().Name,
                    ExceptionMessage = result.Exception?.Message,
                    StackTrace = result.Exception?.StackTrace,
                });
            }
        }
        catch (Exception ex)
        {
            assemblyResults.TestResults.Add(new ModelsWeb.TestResult
            {
                TestClass = "Assembly",
                TestMethod = Path.GetFileName(assemblyPath),
                Status = TestStatus.Failed,
                Messages = [$"Failed to load assembly: {ex.Message}"],
            });
        }

        testRun.AssemblyResults.Add(assemblyResults);
    }
}