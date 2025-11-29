// <copyright file="LazySingleThreadedTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy.Tests;

#pragma warning disable CS1591
#pragma warning disable SA1600
public class LazySingleThreadedTests
{
    [Test]
    public void Constructor_WithNullSupplier_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new LazySingleThreaded<string>(null!); });
    }

    [Test]
    public void Get_OnSubsequentCalls_ShouldReturnsSameValue()
    {
        var callCount = 0;
        var lazy = new LazySingleThreaded<int>(() =>
        {
            callCount++;
            return 42;
        });

        var result1 = lazy.Get();
        var result2 = lazy.Get();
        var result3 = lazy.Get();

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(42));
            Assert.That(result2, Is.EqualTo(42));
            Assert.That(result3, Is.EqualTo(42));
            Assert.That(callCount, Is.EqualTo(1), "Supplier should be called only once");
        });
    }
}