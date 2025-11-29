// <copyright file="LazyCommonTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy.Tests;

#pragma warning disable CS1591
#pragma warning disable SA1600
public class LazyCommonTests
{
    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            yield return new TestCaseData((Func<int>)(() => 42), 42);

            yield return new TestCaseData((Func<string>)(() => "test"), "test");

            var expectedObject = new object();
            yield return new TestCaseData(
                (Func<object>)(() => expectedObject), expectedObject);

            yield return new TestCaseData((Func<string>)(() => null!), null);
        }
    }

    [Test]
    [TestCaseSource(nameof(TestCases))]
    public void Get_Should_ReturnCorrectValue<T>(Func<T> supplier, T expected)
    {
        foreach (var lazy in GetLazyImplementations(supplier))
        {
            var result = lazy.Get();
            Assert.That(result, Is.EqualTo(expected));
        }
    }

    [Test]
    [TestCaseSource(nameof(TestCases))]
    public void Get_Should_ReturnSameValue_OnSubsequentCalls<T>(Func<T> supplier, T expected)
    {
        foreach (var lazy in GetLazyImplementations(supplier))
        {
            var result1 = lazy.Get();
            var result2 = lazy.Get();
            var result3 = lazy.Get();

            Assert.Multiple(() =>
            {
                Assert.That(result1, Is.EqualTo(expected));
                Assert.That(result2, Is.EqualTo(expected));
                Assert.That(result3, Is.EqualTo(expected));
                Assert.That(result3, Is.EqualTo(result1));
            });
        }
    }

    private static IEnumerable<ILazy<T>> GetLazyImplementations<T>(Func<T> supplier)
    {
        return
        [
            new LazySingleThreaded<T>(supplier),
            new LazyMultiThreaded<T>(supplier),
        ];
    }
}