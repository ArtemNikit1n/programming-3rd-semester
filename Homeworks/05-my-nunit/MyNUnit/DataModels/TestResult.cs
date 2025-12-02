// <copyright file="TestResult.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.DataModels;

using System.Collections.Concurrent;

/// <summary>
/// Represents the result of a single test.
/// Contains information about the status, duration, and possible errors.
/// </summary>
public class TestResult(
    string testClass,
    string testMethod,
    TestStatus status,
    TimeSpan duration,
    ConcurrentStack<string> messages,
    Exception? exception)
{
    /// <summary>
    /// Gets the name of the class containing the test.
    /// </summary>
    public string TestClass { get; } = testClass;

    /// <summary>
    /// Gets the name of the test method.
    /// </summary>
    public string TestMethod { get; } = testMethod;

    /// <summary>
    /// Gets the test execution status.
    /// </summary>
    public TestStatus Status { get; } = status;

    /// <summary>
    /// Gets the duration of the test execution.
    /// </summary>
    public TimeSpan Duration { get; } = duration;

    /// <summary>
    /// Gets receives or sets an additional message about the test result.
    /// </summary>
    public ConcurrentStack<string> Messages { get; } = messages;

    /// <summary>
    /// Gets an exception that occurred during the execution of the test.
    /// It can be null if the test is successful.
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// Adds a message.
    /// </summary>
    /// <param name="message">The message being added.</param>
    public void AddMessage(string message)
    {
        this.Messages.Push(message);
    }
}