// <copyright file="MyThreadPoolConcurrencyTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyThreadPool.Tests;

using System.Collections.Concurrent;

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for public visible type or member.
public class MyThreadPoolConcurrencyTests
{
    private MyThreadPool poolWithThreeThreads;

    [SetUp]
    public void CreatePool()
        => this.poolWithThreeThreads = new MyThreadPool(3);

    [TearDown]
    public void DisposePool()
        => this.poolWithThreeThreads.Dispose();

    [Test]
    public void ThreadPool_HandlesMultipleConcurrentSubmits()
    {
        const int taskCount = 100;
        var results = new ConcurrentStack<int>();
        var completionSignal = new CountdownEvent(taskCount);

        for (var i = 0; i < taskCount; i++)
        {
            var taskId = i;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var task = this.poolWithThreeThreads.Submit(() =>
                    {
                        Thread.Sleep(1);
                        return taskId;
                    });

                    results.Push(task.Result);
                }
                finally
                {
                    completionSignal.Signal();
                }
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(completionSignal.Wait(TimeSpan.FromSeconds(5)));
            Assert.That(results, Has.Count.EqualTo(taskCount));
        });
    }

    [Test]
    public void ThreadPool_ExecutesExactlySpecifiedNumberOfThreads()
    {
        const int threadCount = 5;
        var activeThreads = new ConcurrentDictionary<int, byte>();
        var allTasksStarted = new CountdownEvent(threadCount);
        var allowTasksToComplete = new ManualResetEvent(false);

        using var pool = new MyThreadPool(threadCount);

        for (var i = 0; i < threadCount; i++)
        {
            pool.Submit(() =>
            {
                activeThreads.TryAdd(Environment.CurrentManagedThreadId, 0);
                allTasksStarted.Signal();
                allowTasksToComplete.WaitOne();
                return 0;
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(allTasksStarted.Wait(TimeSpan.FromSeconds(5)));
            Assert.That(activeThreads, Has.Count.EqualTo(threadCount));
        });

        allowTasksToComplete.Set();
    }

    [Test]
    public void ContinueWith_ConcurrentWithShutdown_CompletesGracefully()
    {
        var initialTaskReady = new ManualResetEvent(false);
        var canComplete = new ManualResetEvent(false);
        var continuationsCanBeCreated = new CountdownEvent(3);
        var allContinuationsCreated = new CountdownEvent(3);
        var shutdownCanStart = new ManualResetEvent(false);
        var continuationsCreated = 0;
        var successfulContinuations = 0;

        var initialTask = this.poolWithThreeThreads.Submit(() =>
        {
            initialTaskReady.Set();
            canComplete.WaitOne();
            return 100;
        });

        initialTaskReady.WaitOne();

        var continuationThreads = new List<Thread>();
        for (var i = 0; i < 3; i++)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    continuationsCanBeCreated.Signal();
                    shutdownCanStart.WaitOne();
                    var continuation = initialTask.ContinueWith(x =>
                    {
                        Thread.Sleep(5);
                        return x + 1;
                    });
                    Interlocked.Increment(ref continuationsCreated);
                    allContinuationsCreated.Signal();
                    canComplete.Set();
                    _ = continuation.Result;
                    Interlocked.Increment(ref successfulContinuations);
                }
                catch (InvalidOperationException)
                {
                    allContinuationsCreated.Signal();
                }
                catch
                {
                    allContinuationsCreated.Signal();
                }
            });
            continuationThreads.Add(thread);
            thread.Start();
        }

        Assert.That(continuationsCanBeCreated.Wait(TimeSpan.FromSeconds(2)));
        shutdownCanStart.Set();
        Assert.That(allContinuationsCreated.Wait(TimeSpan.FromSeconds(2)));
        canComplete.Set();
        Thread.Sleep(10);
        var shutdownThread = new Thread(() =>
        {
            this.poolWithThreeThreads.Shutdown();
        });
        shutdownThread.Start();

        foreach (var thread in continuationThreads)
        {
            thread.Join(1000);
        }

        shutdownThread.Join(2000);

        var finalCreated = continuationsCreated;
        var finalSuccessful = successfulContinuations;

        Assert.Multiple(() =>
        {
            Assert.That(finalCreated, Is.EqualTo(3));

            Assert.That(finalSuccessful, Is.GreaterThan(0));
        });
    }

    [Test]
    public void ContinueWith_AfterShutdown_ThrowsOnResultAccess()
    {
        var taskStarted = new ManualResetEvent(false);
        var taskCanComplete = new ManualResetEvent(false);

        var initialTask = this.poolWithThreeThreads.Submit(() =>
        {
            taskStarted.Set();
            taskCanComplete.WaitOne();
            return 10;
        });

        taskStarted.WaitOne();

        var shutdownThread = new Thread(() =>
        {
            this.poolWithThreeThreads.Shutdown();
        });
        shutdownThread.Start();

        var continuation = initialTask.ContinueWith(x => x * 2);

        taskCanComplete.Set();

        shutdownThread.Join();

        Assert.Throws<InvalidOperationException>(() => { _ = continuation.Result; });
    }
}