// <copyright file="LazyMultiThreadedTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy.Tests;

using System.Collections.Concurrent;

#pragma warning disable CS1591
#pragma warning disable SA1600
public class LazyMultiThreadedTests
{
    [Test]
    public void Constructor_WithNullSupplier_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = new LazyMultiThreaded<string>(null!); });
    }

    [Test]
    public void Get_OnSubsequentCalls_ShouldReturnsSameValue()
    {
        var callCount = 0;
        var lazy = new LazyMultiThreaded<int>(() =>
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

    [Test]
    public void Get_FromMultipleThreads_ShouldCallSupplierOnlyOnce()
    {
        var callCount = 0;
        var lazy = new LazyMultiThreaded<int>(() =>
        {
            Interlocked.Increment(ref callCount);
            return 42;
        });

        var results = new ConcurrentBag<int>();
        var threads = new Thread[10];

        Barrier barrier = new(threads.Length);

        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                barrier.SignalAndWait();
                results.Add(lazy.Get());
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        Assert.Multiple(() =>
        {
            Assert.That(callCount, Is.EqualTo(1));
            Assert.That(results, Has.Count.EqualTo(10));
            Assert.That(results, Has.All.EqualTo(42));
        });
    }

    [Test]
    public void Get_WithExceptionInSupplier_ShouldPropagateException()
    {
        var lazy = new LazyMultiThreaded<int>(() =>
            throw new InvalidOperationException());

        Assert.Throws<InvalidOperationException>(() => lazy.Get());
    }

    [Test]
    public void Get_AfterException_ShouldThrowSameExceptionOnSubsequentCalls()
    {
        var lazy = new LazyMultiThreaded<int>(() =>
            throw new InvalidOperationException());

        for (var i = 0; i < 3; i++)
        {
            Assert.Throws<InvalidOperationException>(() => lazy.Get());
        }
    }

    [Test]
    public void Get_WithNullResult_ShouldWorkInMultithreadedEnvironment()
    {
        var lazy = new LazyMultiThreaded<string?>(() => null);

        var results = new string?[5];
        Parallel.For(0, 5, i =>
        {
            results[i] = lazy.Get();
        });

        Assert.That(results, Has.All.Null);
    }
}