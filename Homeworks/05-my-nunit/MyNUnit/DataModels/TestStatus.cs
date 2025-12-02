// <copyright file="TestStatus.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.DataModels;

/// <summary>
/// Enumeration of possible test execution statuses.
/// </summary>
public enum TestStatus
{
    /// <summary>
    /// The test was completed successfully.
    /// </summary>
    Passed,

    /// <summary>
    /// The test failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The test was skipped (marked as ignored).
    /// </summary>
    Ignored,
}