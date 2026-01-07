// <copyright file="MyThreadPoolTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyThreadPool.Tests;

using System.Collections.Concurrent;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for public visible type or member.
public class MyThreadPoolTests
{
    private MyThreadPool pool;

    [SetUp]
    public void CreatePool()
        => this.pool = new MyThreadPool(3);

    [TearDown]
    public void DisposePool()
        => this.pool.Dispose();

    [Test]
    public void Constructor_WithNegativeThreads_Should_ThrowArgumentException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new MyThreadPool(-1); });

    [Test]
    public void Constructor_WithZeroThreads_Should_ThrowArgumentException()
        => Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new MyThreadPool(0); });

    [Test]
    public void Constructor_WithPositiveThreads_Should_CreateSpecifiedNumberOfThreads()
    {
        const int threadCount = 5;

        var tasks = new List<IMyTask<int>>();
        var taskCreatedSignal = new CountdownEvent(threadCount * 2);
        var taskCompletedSignal = new CountdownEvent(threadCount * 2);

        for (var i = 0; i < threadCount * 2; i++)
        {
            tasks.Add(this.pool.Submit(() =>
            {
                taskCreatedSignal.Signal();
                taskCompletedSignal.Signal();
                return 42;
            }));
        }

        Assert.Multiple(() =>
        {
            Assert.That(taskCreatedSignal.Wait(TimeSpan.FromSeconds(1)));
            Assert.That(taskCompletedSignal.Wait(TimeSpan.FromSeconds(1)));
        });

        foreach (var task in tasks)
        {
            Assert.That(task.Result, Is.EqualTo(42));
        }
    }

    [Test]
    public void Submit_Should_ExecuteTaskAndReturnResult()
    {
        var task = this.pool.Submit(() => 2 + 3);

        Assert.Multiple(() =>
        {
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.Result, Is.EqualTo(5));
        });

        Assert.That(task.IsCompleted, Is.True);
    }

    [Test]
    public void Submit_WithException_Should_ThrowAggregateExceptionOnResultAccess()
    {
        var task = this.pool.Submit<int>(() => throw new InvalidOperationException("Test exception"));

        Assert.Throws<AggregateException>(() => _ = task.Result);
        Assert.That(task.IsCompleted, Is.True);
    }

    [Test]
    public void Submit_AfterShutdown_Should_ThrowInvalidOperationException()
    {
        this.pool.Shutdown();

        Assert.Throws<InvalidOperationException>(() => this.pool.Submit(() => 42));

        this.pool.Dispose();
    }

    [Test]
    public void ContinueWith_Should_CreateContinuationThatExecutesAfterOriginalTask()
    {
        var task1 = this.pool.Submit(() => 5);
        _ = task1.Result;

        var task2 = task1.ContinueWith(x => x + 1);

        Assert.That(task2.Result, Is.EqualTo(6));
    }

    [Test]
    public void ContinueWith_MultipleContinuations_Should_AllExecute()
    {
        var task1 = this.pool.Submit(() => 10);

        var task2 = task1.ContinueWith(x => x + 1);
        var task3 = task1.ContinueWith(x => x * 2);
        var task4 = task1.ContinueWith(x => x.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(task2.Result, Is.EqualTo(11));
            Assert.That(task3.Result, Is.EqualTo(20));
            Assert.That(task4.Result, Is.EqualTo("10"));
        });
    }

    [Test]
    public void ContinueWith_WithNullFunction_Should_ThrowArgumentNullException()
    {
        var task = this.pool.Submit(() => 42);

        Assert.Throws<ArgumentNullException>(() => task.ContinueWith<int>(null!));
    }

    [Test]
    public void Dispose_Should_CallShutdown()
    {
        this.pool.Dispose();

        Assert.Throws<InvalidOperationException>(() => this.pool.Submit(() => 42));
    }

    [Test]
    public void ContinueWith_DuringShutdown_Should_ThrowInvalidOperationException()
    {
        var task = this.pool.Submit(() => 42);

        _ = task.Result;

        this.pool.Shutdown();

        Assert.Throws<InvalidOperationException>(() =>
            task.ContinueWith(x => x.ToString()));
    }

    [Test]
    public void ContinueWith_Chain_Should_ExecuteInCorrectOrder()
    {
        var results = new ConcurrentQueue<int>();

        var task = this.pool.Submit(() =>
        {
            results.Enqueue(1);
            return 1;
        });

        var task2 = task.ContinueWith(x =>
        {
            results.Enqueue(2);
            return x + 1;
        });

        var task3 = task2.ContinueWith(x =>
        {
            results.Enqueue(3);
            return x + 1;
        });

        _ = task3.Result;

        var resultList = results.ToList();
        Assert.Multiple(() =>
        {
            Assert.That(resultList, Has.Count.EqualTo(3));
            Assert.That(resultList, Is.EqualTo((int[])[1, 2, 3]));
            Assert.That(task3.Result, Is.EqualTo(3));
        });
    }

    [Test]
    public void Submit_NullFunction_Should_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            this.pool.Submit<int>(null!));
    }

    [Test]
    public void Dispose_MultipleTimes_Should_NotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            this.pool.Dispose();
            this.pool.Dispose();
        });
    }

    [Test]
    public void Shutdown_MultipleTimes_Should_NotThrow()
    {
        Assert.DoesNotThrow(() =>
        {
            this.pool.Shutdown();
            this.pool.Shutdown();
        });
    }

    [Test]
    public void ThreadPool_Should_HandleManyTasks()
    {
        const int taskCount = 1000;
        var results = new ConcurrentBag<int>();
        var tasks = new List<IMyTask<int>>();

        for (var i = 0; i < taskCount; i++)
        {
            var localI = i;
            tasks.Add(this.pool.Submit(() =>
            {
                results.Add(localI);
                return localI;
            }));
        }

        foreach (var task in tasks)
        {
            _ = task.Result;
        }

        Assert.That(results, Has.Count.EqualTo(taskCount));
    }

    [Test]
    public void ContinueWith_Should_PropagateExceptions()
    {
        var parentTask = this.pool.Submit<int>(() =>
            throw new InvalidOperationException("Parent failed"));

        var continuation = parentTask.ContinueWith(x => x.ToString());

        Assert.Throws<AggregateException>(() => _ = parentTask.Result);
        Assert.Throws<AggregateException>(() => _ = continuation.Result);
    }
}
