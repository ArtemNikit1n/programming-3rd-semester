// <copyright file="LazyMockTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy.Tests;

using Lazy;
using Moq;

#pragma warning disable CS1591
#pragma warning disable SA1600
public class LazyMockTests
{
    [Test]
    public void Get_Should_ReturnValueFromSupplier_OnFirstCall()
    {
        const int expectedValue = 42;
        var mockLazy = new Mock<ILazy<int>>();
        mockLazy.Setup(l => l.Get()).Returns(expectedValue);

        var result = mockLazy.Object.Get();

        Assert.That(result, Is.EqualTo(expectedValue));
        mockLazy.Verify(l => l.Get(), Times.Once);
    }

    [Test]
    public void Get_Should_ReturnSameValue_OnSubsequentCalls()
    {
        const string expectedValue = "test";
        var mockLazy = new Mock<ILazy<string>>();
        mockLazy.Setup(l => l.Get()).Returns(expectedValue);

        var result1 = mockLazy.Object.Get();
        var result2 = mockLazy.Object.Get();
        var result3 = mockLazy.Object.Get();

        Assert.Multiple(() =>
        {
            Assert.That(result1, Is.EqualTo(expectedValue));
            Assert.That(result2, Is.EqualTo(expectedValue));
            Assert.That(result3, Is.EqualTo(expectedValue));
        });
        mockLazy.Verify(l => l.Get(), Times.Exactly(3));
    }

    [Test]
    public void Get_Should_ReturnNull_WhenSupplierReturnsNull()
    {
        var mockLazy = new Mock<ILazy<string?>>();
        mockLazy.Setup(l => l.Get()).Returns((string?)null);

        var result = mockLazy.Object.Get();

        Assert.That(result, Is.Null);
        mockLazy.Verify(l => l.Get(), Times.Once);
    }

    [Test]
    public void Get_Should_PropagateException_FromSupplier()
    {
        var exception = new InvalidOperationException();
        var mockLazy = new Mock<ILazy<int>>();
        mockLazy.Setup(l => l.Get()).Throws(exception);

        Assert.Throws<InvalidOperationException>(() => mockLazy.Object.Get());
        mockLazy.Verify(l => l.Get(), Times.Once);
    }

    [Test]
    public void Get_Should_BeCalledWithCorrectGenericType()
    {
        var mockLazy = new Mock<ILazy<double>>();
        mockLazy.Setup(l => l.Get()).Returns(3.14);

        var result = mockLazy.Object.Get();

        Assert.That(result, Is.EqualTo(3.14).Within(0.001));
        Assert.That(result, Is.InstanceOf<double>());
    }
}