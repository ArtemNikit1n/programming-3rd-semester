// <copyright file="MyThreadPool.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyThreadPool;

using System.Collections.Concurrent;

/// <summary>
/// A set of pre-created Background threads that are ready to accept tasks for execution.
/// </summary>
public class MyThreadPool : IDisposable
{
    private readonly BlockingCollection<Action> taskQueue;
    private readonly List<Thread> threads;
    private readonly CancellationTokenSource cancellationTokenSource;
    private volatile bool isDisposed;
    private readonly Lock shutdownLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPool"/> class with the specified number of threads.
    /// </summary>
    /// <param name="threadCount">The number of threads in the pool. Must be positive.</param>
    /// <exception cref="ArgumentException">Thrown when threadCount is zero or negative.</exception>
    public MyThreadPool(int threadCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(threadCount, 1);

        this.taskQueue = [];
        this.threads = [];
        this.cancellationTokenSource = new CancellationTokenSource();
        this.isDisposed = false;

        for (var i = 0; i < threadCount; ++i)
        {
            var thread = new Thread(this.WorkerThread)
            {
                Name = $"MyThreadPool Worker #{i}",
                IsBackground = true,
            };
            this.threads.Add(thread);
            thread.Start();
        }
    }

    /// <summary>
    /// Submits a function for execution by the thread pool.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the function.</typeparam>
    /// <param name="function">The function to execute asynchronously.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the thread pool is shutting down.</exception>
    /// <exception cref="ArgumentNullException">Thrown when function is null.</exception>
    public IMyTask<TResult> Submit<TResult>(Func<TResult> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        lock (this.shutdownLock)
        {
            if (this.isDisposed)
            {
                throw new InvalidOperationException("ThreadPool is shutting down.");
            }

            var task = new MyTask<TResult>(function, this);
            this.taskQueue.Add(task.Execute);
            return task;
        }
    }

    /// <summary>
    /// Initiates shutdown of the thread pool. Already running tasks will complete,
    /// but no new tasks will be accepted.
    /// </summary>
    public void Shutdown()
    {
        lock (this.shutdownLock)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;
            this.cancellationTokenSource.Cancel();
            this.taskQueue.CompleteAdding();
        }

        foreach (var thread in this.threads)
        {
            thread.Join();
        }

        this.cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Releases all resources used by the thread pool.
    /// </summary>
    public void Dispose()
        => this.Shutdown();

    private void WorkerThread()
    {
        try
        {
            foreach (var action in this.taskQueue.GetConsumingEnumerable())
            {
                if (this.cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    action();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }
}