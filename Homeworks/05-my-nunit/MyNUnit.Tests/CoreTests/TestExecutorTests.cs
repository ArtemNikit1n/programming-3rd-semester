// <copyright file="TestExecutorTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.Tests.CoreTests;

using System.Reflection;
using MyNUnit.Core;
using MyNUnit.DataModels;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class TestExecutorTests
{
    [SetUp]
    public void Setup()
    {
        SimpleTestClass.BeforeClassWasCalled = false;
        SimpleTestClass.AfterClassWasCalled = false;
    }

    [Test]
    public Task ExecuteTestsAsync_WithNullArgument_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            TestExecutor.ExecuteTestsAsync(null!));
        return Task.CompletedTask;
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldRunTestsFromAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        var results = await TestExecutor.ExecuteTestsAsync(new[] { assemblyPath });

        Assert.That(results, Is.Not.Null);
        Assert.That(results, Is.Not.Empty);

        var testClassResults = results.Where(r => r.TestClass == nameof(SimpleTestClass)).ToList();
        Assert.That(testClassResults, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldCallBeforeClassAndAfterClass()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        Assert.Multiple(() =>
        {
            Assert.That(SimpleTestClass.BeforeClassWasCalled, Is.True);
            Assert.That(SimpleTestClass.AfterClassWasCalled, Is.True);
        });
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldCallBeforeAndAfterForEachTest()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        Assert.Multiple(() =>
        {
            Assert.That(SimpleTestClass.BeforeWasCalledCount, Is.EqualTo(3));
            Assert.That(SimpleTestClass.AfterWasCalledCount, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldIgnoreTestWithIgnoreReason()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        var results = await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        var ignoredTestResult = results.FirstOrDefault(r =>
            r is { TestClass: nameof(SimpleTestClass), TestMethod: nameof(SimpleTestClass.IgnoredTest) });

        Assert.That(ignoredTestResult, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ignoredTestResult.Status, Is.EqualTo(TestStatus.Ignored));
            Assert.That(ignoredTestResult.Messages, Contains.Item($"Ignored: The test was ignored for a reason"));
        });
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldPassTestWithExpectedException()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        var results = await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        var testResult = results.FirstOrDefault(r =>
            r is { TestClass: nameof(SimpleTestClass), TestMethod: nameof(SimpleTestClass.TestWithExpectedException) });

        Assert.That(testResult, Is.Not.Null);
        Assert.That(testResult.Status, Is.EqualTo(TestStatus.Passed));
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldFailTestWithWrongException()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        var results = await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        var testResult = results.FirstOrDefault(r =>
            r is { TestClass: nameof(SimpleTestClass), TestMethod: nameof(SimpleTestClass.TestWithWrongException) });

        Assert.That(testResult, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(testResult.Status, Is.EqualTo(TestStatus.Failed));
            Assert.That(
                testResult.Messages.Any(m =>
                m.Contains("Expected exception InvalidOperationException") &&
                m.Contains("ArgumentException")),
                Is.True);
        });
    }

    [Test]
    public async Task ExecuteTestsAsync_WhenAfterClassThrowsException_ShouldAddWarningMessage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        var results = await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        var testResult = results.First();
        Assert.That(testResult.Messages, Contains.Item($"\nWarning: AfterClass failed: AfterClass exception"));
    }

    [Test]
    public async Task ExecuteTestsAsync_WhenAfterThrowsException_ShouldAddWarningMessage()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        await TestExecutor.ExecuteTestsAsync([assemblyPath]);

        Assert.That(SimpleTestClass.AfterExceptionThrown, Is.True);
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldHandleMultipleAssemblies()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;

        var results = await TestExecutor.ExecuteTestsAsync([assemblyPath, assemblyPath]);

        Assert.That(results, Is.Not.Null);
        Assert.That(results, Is.Not.Empty);
    }

    [Test]
    public async Task ExecuteTestsAsync_ShouldSkipNonExistentOrNonDllFiles()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyPath = assembly.Location;
        const string nonExistentPath = "nonexistent.dll";
        const string textFilePath = "test.txt";

        await File.WriteAllTextAsync(textFilePath, "test");

        try
        {
            var results = await TestExecutor.ExecuteTestsAsync([
                assemblyPath,
                nonExistentPath,
                textFilePath
            ]);

            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Not.Empty);
        }
        finally
        {
            if (File.Exists(textFilePath))
            {
                File.Delete(textFilePath);
            }
        }
    }
}