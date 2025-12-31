// <copyright file="TestsController.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnitWeb;

using Microsoft.AspNetCore.Mvc;

// ReSharper disable once RedundantUsingDirective
using MyNUnitWeb.ModelsWeb;

/// <summary>
/// API controller for managing test assemblies, executing tests, and retrieving test run history.
/// Provides endpoints for uploading DLL assemblies, running tests, and managing test execution history.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    private readonly TestService testService;
    private readonly ILogger<TestsController> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestsController"/> class.
    /// </summary>
    /// <param name="testService">The test service for executing and managing tests.</param>
    /// <param name="logger">The logger for recording controller events.</param>
    public TestsController(TestService testService, ILogger<TestsController> logger)
    {
        this.testService = testService;
        this.logger = logger;
    }

    /// <summary>
    /// Uploads a DLL assembly to the server for testing.
    /// </summary>
    /// <param name="file">The DLL file to upload.</param>
    /// <returns>The server path to the uploaded file.</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> UploadAssembly(IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length == 0)
        {
            return this.BadRequest(new { error = "No file or empty file" });
        }

        try
        {
            var filePath = await this.testService.UploadAssembly(file);
            return this.Ok(new { filePath });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error uploading assembly");
            return this.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Executes tests in the specified assemblies.
    /// </summary>
    /// <param name="assemblyPaths">List of server paths to assemblies to test.</param>
    /// <returns>The result of the test run.</returns>
    [HttpPost("run")]
    public async Task<IActionResult> RunTests([FromBody] List<string> assemblyPaths)
    {
        try
        {
            var testRun = await this.testService.RunTests(assemblyPaths);
            return this.Ok(testRun);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error running tests");
            return this.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the history of all test runs.
    /// </summary>
    /// <returns>A list of test runs sorted by time (newest first).</returns>
    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        try
        {
            var history = this.testService.GetTestRunHistory();
            return this.Ok(history);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting history");
            return this.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a specific test run by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the test run.</param>
    /// <returns>The requested test run or NotFound if not found.</returns>
    [HttpGet("history/{id}")]
    public IActionResult GetTestRun(Guid id)
    {
        try
        {
            var testRun = this.testService.GetTestRun(id);
            if (testRun == null)
            {
                return this.NotFound();
            }

            return this.Ok(testRun);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error getting test run");
            return this.BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Clears all test run history from the server.
    /// </summary>
    /// <returns>OK if successful.</returns>
    [HttpDelete("history")]
    public IActionResult ClearHistory()
    {
        try
        {
            this.testService.ClearHistory();
            return this.Ok();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error clearing history");
            return this.BadRequest(new { error = ex.Message });
        }
    }
}