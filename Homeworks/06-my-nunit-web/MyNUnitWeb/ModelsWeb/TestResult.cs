// <copyright file="TestResult.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnitWeb.ModelsWeb;

using MyNUnit.DataModels;

/// <summary>
/// Represents the result of a single test execution for Web API serialization.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Gets or sets the name of the class containing the test.
    /// </summary>
    public string TestClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the test method.
    /// </summary>
    public string TestMethod { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution status of the test.
    /// </summary>
    public TestStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the duration of test execution in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the list of informational messages related to the test execution.
    /// </summary>
    public List<string> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the type of exception that occurred during test execution, if any.
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the message of the exception that occurred during test execution.
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Gets or sets the stack trace of the exception that occurred during test execution.
    /// </summary>
    public string? StackTrace { get; set; }
}