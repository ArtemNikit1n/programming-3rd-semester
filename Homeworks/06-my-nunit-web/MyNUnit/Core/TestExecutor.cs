// <copyright file="TestExecutor.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.Core;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

// ReSharper disable once RedundantNameQualifier
using MyNUnit.DataModels;

/// <summary>
/// Performs the detected tests and collects the results.
/// Manages the lifecycle of tests: calling Before/After methods, exception handling.
/// </summary>
public static class TestExecutor
{
    /// <summary>
    /// Executes all tests in the specified builds asynchronously.
    /// </summary>
    /// <param name="assemblyPaths">Paths to assemblies containing tests.</param>
    /// <returns>An asynchronous execution task that returns a list of test results.</returns>
    /// <exception cref="ArgumentNullException">It is thrown if <paramref name="assemblyPaths"/> is null.</exception>
    public static async Task<List<TestResult>> ExecuteTestsAsync(IEnumerable<string> assemblyPaths)
    {
        ArgumentNullException.ThrowIfNull(assemblyPaths);
        var allTests = new List<TestInfo>();

        foreach (var assemblyPath in assemblyPaths)
        {
            if (!File.Exists(assemblyPath) || !Path.GetExtension(assemblyPath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var assembly = Assembly.LoadFrom(assemblyPath);
            allTests.AddRange(TestDiscoverer.DiscoverTests(assembly));
        }

        var testsByClass = allTests.GroupBy(t => t.TestClass).ToList();
        var classTasks = testsByClass.Select(async classTests
            => await RunTestsInClassAsync(classTests.ToList()));

        var classResults = await Task.WhenAll(classTasks);
        return classResults.SelectMany(r => r).ToList();
    }

    private static async Task<ConcurrentBag<TestResult>> RunTestsInClassAsync(List<TestInfo> testsInClass)
    {
        ArgumentNullException.ThrowIfNull(testsInClass);
        var results = new ConcurrentBag<TestResult>();
        if (testsInClass.Count == 0)
        {
            return results;
        }

        var beforeClassMethod = testsInClass.First().BeforeClassMethod;
        var afterClassMethod = testsInClass.First().AfterClassMethod;

        try
        {
            beforeClassMethod?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            var actualException = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            foreach (var testResult in testsInClass.Select(testInfo => new TestResult(
                         testInfo.TestClass.Name,
                         testInfo.TestMethod.Name,
                         TestStatus.Failed,
                         TimeSpan.Zero,
                         new ConcurrentStack<string>([$"BeforeClass failed: {actualException.Message}"]),
                         actualException)))
            {
                results.Add(testResult);
            }

            return results;
        }

        var testTasks = testsInClass.Select(testInfo => Task.Run(() => RunTestAsync(testInfo).Result)).ToList();

        var testResults = await Task.WhenAll(testTasks);
        foreach (var testResult in testResults)
        {
            results.Add(testResult);
        }

        try
        {
            afterClassMethod?.Invoke(null, null);
        }
        catch (Exception ex)
        {
            var actualException = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            foreach (var result in results)
            {
                result.AddMessage($"\nWarning: AfterClass failed: {actualException.Message}");
            }
        }

        return results;
    }

    private static async Task<TestResult> RunTestAsync(TestInfo testInfo)
    {
        ArgumentNullException.ThrowIfNull(testInfo);
        var testClass = testInfo.TestClass.Name;
        var testMethod = testInfo.TestMethod.Name;

        if (!string.IsNullOrEmpty(testInfo.IgnoreReason))
        {
            return new TestResult(
                testClass,
                testMethod,
                TestStatus.Ignored,
                TimeSpan.Zero,
                new ConcurrentStack<string>([$"Ignored: {testInfo.IgnoreReason}"]),
                null);
        }

        var stopwatch = Stopwatch.StartNew();
        object? testInstance = null;
        if (!testInfo.TestMethod.IsStatic)
        {
            testInstance = Activator.CreateInstance(testInfo.TestClass);
        }

        ConcurrentStack<string> testMessages = [];
        TestStatus testStatus;
        try
        {
            var beforeMethodsResult = await ExecuteBeforeMethodsAsync(testInfo, testInstance, stopwatch);
            if (beforeMethodsResult != null)
            {
                return beforeMethodsResult;
            }

            await Task.Run(() => testInfo.TestMethod.Invoke(testInstance, null));
            testStatus = testInfo.ExpectedException != null ? TestStatus.Failed : TestStatus.Passed;
            if (testInfo.ExpectedException != null)
            {
                testMessages.Push($"Expected exception {testInfo.ExpectedException.Name} was not thrown");
            }

            stopwatch.Stop();
        }
        catch (Exception ex)
        {
            return HandleTestException(ex, testInfo, stopwatch);
        }
        finally
        {
            await ExecuteAfterMethodsAsync(testMessages, testInstance, testInfo.AfterMethods);
            if (testInstance is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        return new TestResult(
            testClass,
            testMethod,
            testStatus,
            stopwatch.Elapsed,
            testMessages,
            null);
    }

    private static async Task<TestResult?> ExecuteBeforeMethodsAsync(TestInfo testInfo, object? testInstance, Stopwatch stopwatch)
    {
        try
        {
            foreach (var beforeMethod in testInfo.BeforeMethods)
            {
                await Task.Run(() => beforeMethod.Invoke(testInstance, null));
            }
        }
        catch (Exception ex)
        {
            var actualException = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            stopwatch.Stop();
            return new TestResult(
                testInfo.TestClass.Name,
                testInfo.TestMethod.Name,
                TestStatus.Failed,
                stopwatch.Elapsed,
                new ConcurrentStack<string>([$"Before method failed: {actualException.GetType().Name}"]),
                actualException);
        }

        return null;
    }

    private static async Task ExecuteAfterMethodsAsync(ConcurrentStack<string> result, object? testInstance, List<MethodInfo> afterMethods)
    {
        try
        {
            foreach (var afterMethod in afterMethods)
            {
                await Task.Run(() => afterMethod.Invoke(testInstance, null));
            }
        }
        catch (Exception ex)
        {
            var actualException = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            result.Push($"\nWarning: After method failed: {actualException.Message}");
        }
    }

    private static TestResult HandleTestException(Exception exception, TestInfo testInfo, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        var actualException = exception is TargetInvocationException tie ? tie.InnerException ?? exception : exception;
        var testStatus = TestStatus.Failed;
        ConcurrentStack<string> testMessages = [];
        if (testInfo.ExpectedException != null)
        {
            if (testInfo.ExpectedException.IsInstanceOfType(actualException))
            {
                testStatus = TestStatus.Passed;
            }
            else
            {
                testMessages.Push(
                    $"Expected exception {testInfo.ExpectedException.Name} but got {actualException.GetType().Name}: " +
                    $"{actualException.Message}");
            }
        }
        else
        {
            testMessages.Push(
                $"Unexpected exception: {actualException.Message}");
        }

        return new TestResult(
            testInfo.TestClass.Name,
            testInfo.TestMethod.Name,
            testStatus,
            stopwatch.Elapsed,
            testMessages,
            actualException);
    }
}