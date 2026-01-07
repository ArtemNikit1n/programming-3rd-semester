// <copyright file="MyTask.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyThreadPool;

/// <inheritdoc/>
/// <summary>
/// Initializes a new instance of the <see cref="MyTask{TResult}"/> class.
/// </summary>
/// <param name="function">The function to execute asynchronously.</param>
/// <param name="threadPool">The thread pool that will execute this task.</param>
internal class MyTask<TResult>(Func<TResult> function, MyThreadPool threadPool) : IMyTask<TResult>
{
    private readonly Func<TResult> function = function ?? throw new ArgumentNullException(nameof(function));
    private readonly MyThreadPool threadPool = threadPool ?? throw new ArgumentNullException(nameof(threadPool));
    private readonly ManualResetEvent completionEvent = new(false);
    private readonly Lock continuationLock = new();
    private readonly List<Action> continuations = [];
    private volatile bool continuationsStarted;
    private TResult? result;
    private Exception? exception;
    private volatile bool isCompleted;

    /// <inheritdoc/>
    public bool IsCompleted
        => this.isCompleted;

    /// <inheritdoc/>
    public TResult Result
    {
        get
        {
            this.completionEvent.WaitOne();

            if (this.exception != null)
            {
                throw new AggregateException(this.exception);
            }

            return this.result ?? throw new InvalidOperationException("Task completed without result");
        }
    }

    /// <summary>
    /// Executes the task function and handles completion logic including continuations.
    /// </summary>
    public void Execute()
    {
        try
        {
            this.result = this.function();
        }
        catch (Exception ex)
        {
            this.exception = ex;
        }
        finally
        {
            this.isCompleted = true;
            this.completionEvent.Set();
            this.StartContinuations();
        }
    }

    /// <inheritdoc/>
    public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> newFunction)
    {
        ArgumentNullException.ThrowIfNull(newFunction);

        if (this.isCompleted)
        {
            return this.threadPool.Submit(() => newFunction(this.Result));
        }

        lock (this.continuationLock)
        {
            if (this.isCompleted)
            {
                return this.threadPool.Submit(() => newFunction(this.Result));
            }

            var continuation = new DeferredTask<TResult, TNewResult>(
                this,
                newFunction,
                this.threadPool);

            this.continuations.Add(continuation.Start);
            return continuation;
        }
    }

    private void StartContinuations()
    {
        if (this.continuationsStarted)
        {
            return;
        }

        lock (this.continuationLock)
        {
            if (this.continuationsStarted)
            {
                return;
            }

            this.continuationsStarted = true;
            foreach (var continuation in this.continuations)
            {
                continuation();
            }
        }
    }

    private class DeferredTask<TSource, TNewResult>(
        MyTask<TSource> parentTask,
        Func<TSource, TNewResult> function,
        MyThreadPool threadPool) : IMyTask<TNewResult>
    {
        private readonly Lazy<IMyTask<TNewResult>> lazyTask = new(() => threadPool.Submit(() => function(parentTask.Result)));

        public bool IsCompleted
            => this.lazyTask.Value.IsCompleted;

        public TNewResult Result
            => this.lazyTask.Value.Result;

        public IMyTask<TNextResult> ContinueWith<TNextResult>(Func<TNewResult, TNextResult> function)
            => this.lazyTask.Value.ContinueWith(function);

        public void Start()
        {
            _ = this.lazyTask.Value;
        }
    }
}