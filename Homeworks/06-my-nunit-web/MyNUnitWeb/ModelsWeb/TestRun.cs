// <copyright file="TestRun.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnitWeb.ModelsWeb;

/// <summary>
/// This is the root model, representing one complete test run.
/// </summary>
public class TestRun
{
    /// <summary>
    /// Gets or sets unique identifier for launching links and searches.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the time the launch was performed.
    /// </summary>
    public DateTime RunTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets results for each tested build.
    /// </summary>
    public List<AssemblyTestResult> AssemblyResults { get; } = [];
}
