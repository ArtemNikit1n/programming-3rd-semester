// <copyright file="TestReporter.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit;

using MyNUnit.DataModels;

/// <summary>
/// Generates reports on test results.
/// Provides formatted output of test results.
/// </summary>
public static class TestReporter
{
    private const int NumberOfCharactersInSeparator = 60;

    /// <summary>
    /// Generates a detailed report on the test results.
    /// </summary>
    /// <param name="results">List of test results.</param>
    /// <param name="output">The report output stream.</param>
    public static void GenerateReport(List<TestResult> results, TextWriter output)
    {
        var passed = results.Count(r => r.Status == TestStatus.Passed);
        var failed = results.Count(r => r.Status == TestStatus.Failed);
        var ignored = results.Count(r => r.Status == TestStatus.Ignored);
        var total = results.Count;

        output.WriteLine(new string('=', NumberOfCharactersInSeparator));
        output.WriteLine("TEST EXECUTION REPORT");
        output.WriteLine(new string('=', NumberOfCharactersInSeparator));
        output.WriteLine($"Total: {total} | Passed: {passed} | Failed: {failed} | Ignored: {ignored}");
        output.WriteLine();

        foreach (var classGroup in results.GroupBy(r => r.TestClass))
        {
            output.WriteLine($"CLASS: {classGroup.Key}");
            output.WriteLine(new string('-', NumberOfCharactersInSeparator));

            foreach (var result in classGroup)
            {
                var statusIcon = result.Status switch
                {
                    TestStatus.Passed => "✓",
                    TestStatus.Failed => "✗",
                    TestStatus.Ignored => "~",
                    _ => "?",
                };

                output.WriteLine($"{statusIcon} {result.TestMethod} [{result.Duration.TotalMilliseconds:F2}ms]");

                if (result.Messages.Count > 0)
                {
                    foreach (var message in result.Messages)
                    {
                        output.WriteLine($"   {message}");
                    }
                }

                if (result.Exception != null)
                {
                    output.WriteLine($"   {result.Exception.GetType().Name}: {result.Exception.Message}");
                }
            }

            output.WriteLine();
        }

        var failedTests = results.Where(r => r.Status == TestStatus.Failed).ToList();
        if (failedTests.Count != 0)
        {
            output.WriteLine("FAILED TESTS DETAILS:");
            output.WriteLine(new string('=', NumberOfCharactersInSeparator));

            foreach (var failedTest in failedTests)
            {
                output.WriteLine($"{failedTest.TestClass}.{failedTest.TestMethod}:");
                output.WriteLine($"  {failedTest.Messages}");
                if (failedTest.Exception is { StackTrace: not null })
                {
                    var stackTraceLines = failedTest.Exception.StackTrace.Split('\n')
                        .Take(5)
                        .Select(line => $"  {line.Trim()}");
                    output.WriteLine(string.Join(Environment.NewLine, stackTraceLines));
                }

                output.WriteLine();
            }
        }

        output.WriteLine(new string('=', NumberOfCharactersInSeparator));
        output.WriteLine(failed == 0 ? "ALL TESTS PASSED!" : $"FAILED: {failed} test(s)");
        output.WriteLine(new string('=', NumberOfCharactersInSeparator));
    }
}