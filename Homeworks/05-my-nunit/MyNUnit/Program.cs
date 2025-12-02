// <copyright file="Program.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.CommandLine;
using MyNUnit;
using MyNUnit.Core;
using MyNUnit.DataModels;

Argument<string> pathArgument = new(
    name: "path",
    description: "Path to a .dll file or directory containing assemblies to test.",
    getDefaultValue: Directory.GetCurrentDirectory);

Option<string> patternOption = new(
    name: "--pattern",
    description: "Search pattern for assembly files.",
    getDefaultValue: () => "*.dll");

Option<bool> recursiveOption = new(
    name: "--recursive",
    description: "Search for assemblies in subdirectories recursively.");

RootCommand rootCommand = new("Custom test runner for .NET assemblies");
rootCommand.AddArgument(pathArgument);
rootCommand.AddOption(patternOption);
rootCommand.AddOption(recursiveOption);

rootCommand.SetHandler(RunTests, pathArgument, patternOption, recursiveOption);
return await rootCommand.InvokeAsync(args);

static async Task RunTests(string path, string pattern, bool recursive)
{
    try
    {
        Console.WriteLine($"Searching for tests in: {path}");
        Console.WriteLine($"Pattern: {pattern}, Recursive: {recursive}");

        var assemblies = DiscoverAssemblies(path, pattern, recursive);
        var assemblyPaths = assemblies as string[] ?? assemblies.ToArray();

        Console.WriteLine($"Found {assemblyPaths.Length} assemblies:");
        foreach (var assemblyPath in assemblyPaths)
        {
            Console.WriteLine($"  {Path.GetFileName(assemblyPath)}");
        }

        Console.WriteLine();

        var results = await TestExecutor.ExecuteTestsAsync(assemblyPaths);

        TestReporter.GenerateReport(results, Console.Out);

        var hasFailures = results.Any(r => r.Status == TestStatus.Failed);
        Environment.Exit(hasFailures ? 1 : 0);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Environment.Exit(2);
    }
}

static IEnumerable<string> DiscoverAssemblies(string path, string pattern, bool recursive)
{
    var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

    if (File.Exists(path) && Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
    {
        return [path];
    }

    if (Directory.Exists(path))
    {
        return Directory.GetFiles(path, pattern, searchOption)
            .Where(f => Path.GetExtension(f).Equals(".dll", StringComparison.OrdinalIgnoreCase));
    }

    throw new FileNotFoundException($"Path not found: {path}");
}