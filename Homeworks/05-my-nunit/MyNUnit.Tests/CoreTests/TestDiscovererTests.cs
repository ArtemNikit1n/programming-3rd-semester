// <copyright file="TestDiscovererTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.Tests.CoreTests;

using System.Reflection;

// ReSharper disable once RedundantNameQualifier
using MyNUnit.Core;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class TestDiscovererTests
{
    [Test]
    public void DiscoverTests_WithNullAssembly_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            foreach (var discoverTest in TestDiscoverer.DiscoverTests(null!))
            {
                _ = discoverTest;
            }
        });
    }

    [Test]
    public void DiscoverTests_ShouldFindTestMethods()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tests = TestDiscoverer.DiscoverTests(assembly).ToList();

        Assert.That(tests, Is.Not.Null);
        Assert.That(tests, Is.Not.Empty);
    }

    [Test]
    public void DiscoverTests_ShouldParseTestAttributesCorrectly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tests = TestDiscoverer.DiscoverTests(assembly).ToList();

        var test = tests.FirstOrDefault(t => t.TestMethod.Name == "IgnoredTest");
        Assert.That(test, Is.Not.Null);
        Assert.That(test.IgnoreReason, Is.EqualTo("The test was ignored for a reason"));

        var exceptionTest = tests.FirstOrDefault(t => t.TestMethod.Name == "TestWithExpectedException");
        Assert.That(exceptionTest, Is.Not.Null);
        Assert.That(exceptionTest.ExpectedException, Is.EqualTo(typeof(InvalidOperationException)));
    }

    [Test]
    public void DiscoverTests_ShouldFindBeforeAfterMethods()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var tests = TestDiscoverer.DiscoverTests(assembly).ToList();

        var test = tests.First();

        Assert.That(test.BeforeClassMethod, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(test.BeforeClassMethod.Name, Is.EqualTo("BeforeClass"));

            Assert.That(test.AfterClassMethod, Is.Not.Null);
        });
        Assert.Multiple(() =>
        {
            Assert.That(test.AfterClassMethod.Name, Is.EqualTo("AfterClass"));

            Assert.That(test.BeforeMethods, Has.Count.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(test.BeforeMethods[0].Name, Is.EqualTo("Before"));

            Assert.That(test.AfterMethods, Has.Count.EqualTo(2));
        });
        Assert.That(test.AfterMethods[0].Name, Is.EqualTo("After"));
    }
}