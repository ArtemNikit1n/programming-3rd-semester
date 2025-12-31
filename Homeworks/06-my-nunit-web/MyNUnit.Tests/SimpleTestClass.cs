// <copyright file="SimpleTestClass.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.Tests;

using MyNUnit.UsersAttributes;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class SimpleTestClass
{
    public static bool BeforeClassWasCalled { get; set; }

    public static bool AfterClassWasCalled { get; set; }

    public static int BeforeWasCalledCount { get; private set; }

    public static int AfterWasCalledCount { get; private set; }

    public static bool AfterExceptionThrown { get; private set; }

    [BeforeClass]
    public static void BeforeClass()
    {
        BeforeClassWasCalled = true;
    }

    [AfterClass]
    public static void AfterClass()
    {
        AfterClassWasCalled = true;
        throw new InvalidOperationException("AfterClass exception");
    }

    [Before]
    public void Before()
    {
        BeforeWasCalledCount++;
    }

    [After]
    public void After()
    {
        AfterWasCalledCount++;
    }

    [After]
    public void AfterException()
    {
        AfterWasCalledCount++;
        if (AfterExceptionThrown)
        {
            return;
        }

        AfterExceptionThrown = true;
        throw new InvalidOperationException("After exception");
    }

    [Test(null, null)]
    public void PassingTest()
    {
    }

    [Test("The test was ignored for a reason", null)]
    public void IgnoredTest()
    {
    }

    [Test(null, typeof(InvalidOperationException))]
    public void TestWithExpectedException()
    {
        throw new InvalidOperationException("Expected exception");
    }

    [Test(null, typeof(InvalidOperationException))]
    public void TestWithWrongException()
    {
        throw new ArgumentException("Wrong exception");
    }
}