// <copyright file="TestReporterTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.Tests;

using System.Collections.Concurrent;
using MyNUnit.DataModels;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class TestReporterTests
{
    [Test]
    public void GenerateReport_ShouldOutputCorrectFormat()
    {
        var results = new List<TestResult>
        {
            new TestResult(
                "TestClass1",
                "TestMethod1",
                TestStatus.Passed,
                TimeSpan.FromMilliseconds(150),
                [],
                null),
            new TestResult(
                "TestClass1",
                "TestMethod2",
                TestStatus.Failed,
                TimeSpan.FromMilliseconds(200),
                new ConcurrentStack<string>(["Test failed"]),
                new InvalidOperationException("Error occurred")),
            new TestResult(
                "TestClass2",
                "TestMethod3",
                TestStatus.Ignored,
                TimeSpan.Zero,
                new ConcurrentStack<string>(["Ignored: Reason"]),
                null),
        };

        using var writer = new StringWriter();
        TestReporter.GenerateReport(results, writer);
        var output = writer.ToString();

        Assert.That(output, Contains.Substring("TEST EXECUTION REPORT"));
        Assert.That(output, Contains.Substring("Total: 3 | Passed: 1 | Failed: 1 | Ignored: 1"));
        Assert.That(output, Contains.Substring("✓"));
        Assert.That(output, Contains.Substring("✗"));
        Assert.That(output, Contains.Substring("~"));
        Assert.That(output, Contains.Substring("FAILED TESTS DETAILS:"));
    }

    [Test]
    public void GenerateReport_ShouldHandleEmptyResults()
    {
        var results = new List<TestResult>();
        using var writer = new StringWriter();

        TestReporter.GenerateReport(results, writer);
        var output = writer.ToString();

        Assert.That(output, Contains.Substring("Total: 0 | Passed: 0 | Failed: 0 | Ignored: 0"));
    }

    [Test]
    public void GenerateReport_ShouldGroupResultsByClass()
    {
        var results = new List<TestResult>
        {
            new TestResult("Class1", "Method1", TestStatus.Passed, TimeSpan.Zero, [], null),
            new TestResult("Class1", "Method2", TestStatus.Passed, TimeSpan.Zero, [], null),
            new TestResult("Class2", "Method1", TestStatus.Passed, TimeSpan.Zero, [], null),
        };

        using var writer = new StringWriter();
        TestReporter.GenerateReport(results, writer);
        var output = writer.ToString();

        Assert.That(output, Contains.Substring("CLASS: Class1"));
        Assert.That(output, Contains.Substring("CLASS: Class2"));
    }

    [Test]
    public void GenerateReport_ShouldDisplayExceptionStackTrace()
    {
        var exception = new InvalidOperationException("Test exception")
        {
            Data =
            {
                ["StackTrace"] = "at TestMethod()\n  at AnotherMethod()",
            },
        };

        var results = new List<TestResult>
        {
            new TestResult(
                "TestClass",
                "TestMethod",
                TestStatus.Failed,
                TimeSpan.Zero,
                [],
                exception),
        };

        using var writer = new StringWriter();
        TestReporter.GenerateReport(results, writer);
        var output = writer.ToString();

        Assert.That(output, Contains.Substring("InvalidOperationException"));
        Assert.That(output, Contains.Substring("Test exception"));
    }
}