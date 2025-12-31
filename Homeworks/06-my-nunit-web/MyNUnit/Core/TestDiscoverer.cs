// <copyright file="TestDiscoverer.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.Core;

using System.Reflection;
using MyNUnit.DataModels;
using MyNUnit.UsersAttributes;

/// <summary>
/// Responsible for detecting tests in assemblies.
/// Analyzes types and methods in the assembly, finds methods with testing attributes.
/// </summary>
public static class TestDiscoverer
{
    /// <summary>
    /// Detects all tests in the specified assembly.
    /// </summary>
    /// <param name="assembly">Assembly for analysis.</param>
    /// <returns>Enumeration of <see cref="TestInfo"/> objects representing the found tests.</returns>
    /// <exception cref="ArgumentNullException">It is thrown if <paramref name="assembly"/> is null.</exception>
    public static IEnumerable<TestInfo> DiscoverTests(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        foreach (var type in assembly.GetTypes())
        {
            var testMethods = type.GetMethods()
                .Where(m => m.GetCustomAttribute<TestAttribute>() != null).ToList();
            if (testMethods.Count == 0)
            {
                continue;
            }

            var beforeClassMethod = type.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<BeforeClassAttribute>() != null && m.IsStatic);
            var afterClassMethod = type.GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<AfterClassAttribute>() != null && m.IsStatic);
            var beforeMethods = type.GetMethods()
                .Where(m => m.GetCustomAttribute<BeforeAttribute>() != null && !m.IsStatic).ToList();
            var afterMethods = type.GetMethods()
                .Where(m => m.GetCustomAttribute<AfterAttribute>() != null && !m.IsStatic).ToList();
            foreach (var testMethod in testMethods)
            {
                var testAttribute = testMethod.GetCustomAttribute<TestAttribute>();
                if (testAttribute == null)
                {
                    continue;
                }

                yield return new TestInfo(
                    type,
                    testMethod,
                    beforeClassMethod,
                    afterClassMethod,
                    beforeMethods,
                    afterMethods,
                    testAttribute.Ignore,
                    testAttribute.ExpectedException);
            }
        }
    }
}