// <copyright file="AssemblyTestResult.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnitWeb.ModelsWeb;

using MyNUnit.DataModels;

/// <summary>
/// A model for the test results of one specific DLL assembly.
/// </summary>
public class AssemblyTestResult
{
    /// <summary>
    /// Gets or sets the file name of the assembly (e.g. "MyTests.dll").
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full path to the assembly file on the server.
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the results of all tests in this assembly.
    /// </summary>
    public List<TestResult> TestResults { get; set; } = [];

    /// <summary>
    /// Gets the total number of tests in this assembly.
    /// </summary>
    public int TotalTests => this.TestResults.Count;

    /// <summary>
    /// Gets the number of tests that passed in this assembly.
    /// </summary>
    public int Passed => this.TestResults.Count(r => r.Status == TestStatus.Passed);

    /// <summary>
    /// Gets the number of tests that failed in this assembly.
    /// </summary>
    public int Failed => this.TestResults.Count(r => r.Status == TestStatus.Failed);

    /// <summary>
    /// Gets the number of tests that were ignored in this assembly.
    /// </summary>
    public int Ignored => this.TestResults.Count(r => r.Status == TestStatus.Ignored);
}