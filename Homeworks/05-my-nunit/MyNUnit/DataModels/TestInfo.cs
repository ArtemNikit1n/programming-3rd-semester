// <copyright file="TestInfo.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.DataModels;

using System.Reflection;

/// <summary>
/// Contains information about the test found in the build.
/// Includes the metadata of the test and related methods (Before, After, etc.).
/// </summary>
public class TestInfo(
    Type testClass,
    MethodInfo testMethod,
    MethodInfo? beforeClassMethod,
    MethodInfo? afterClassMethod,
    List<MethodInfo> beforeMethods,
    List<MethodInfo> afterMethods,
    string? ignoreReason,
    Type? expectedException)
{
    /// <summary>
    /// Gets the type of the class containing the test.
    /// </summary>
    public Type TestClass { get; } = testClass;

    /// <summary>
    /// Gets the method representing the test.
    /// </summary>
    public MethodInfo TestMethod { get; } = testMethod;

    /// <summary>
    /// Gets a static method executed before all tests in the class.
    /// </summary>
    public MethodInfo? BeforeClassMethod { get; } = beforeClassMethod;

    /// <summary>
    /// Gets a static method executed after all tests in the class.
    /// </summary>
    public MethodInfo? AfterClassMethod { get; } = afterClassMethod;

    /// <summary>
    /// Gets a list of methods executed before each test.
    /// </summary>
    public List<MethodInfo> BeforeMethods { get; } = beforeMethods;

    /// <summary>
    /// Gets a list of methods to be executed after each test.
    /// </summary>
    public List<MethodInfo> AfterMethods { get; } = afterMethods;

    /// <summary>
    /// Gets the reason for ignoring the test.
    /// </summary>
    public string? IgnoreReason { get; } = ignoreReason;

    /// <summary>
    /// Gets the type of exception that is expected from the test.
    /// </summary>
    public Type? ExpectedException { get; } = expectedException;
}