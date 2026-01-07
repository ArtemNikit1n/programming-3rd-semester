// <copyright file="IMyTask.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyThreadPool;

/// <summary>
/// Represents an asynchronous operation that can return a value.
/// Provides functionality for continuations and result retrieval.
/// </summary>
/// <typeparam name="TResult">The type of the result produced by the task.</typeparam>
public interface IMyTask<out TResult>
{
    /// <summary>
    /// Gets a value indicating whether true if the task is completed. If the result is not yet ready, it returns false.
    /// </summary>
    public bool IsCompleted { get; }

    /// <summary>
    /// Gets the result of task execution.
    /// If the function corresponding to the task returns with an exception, this method will return with an AggregateException containing the exception that caused the problem.
    /// If the result has not yet been calculated, the method waits for it and returns the resulting value, blocking the calling thread.
    /// </summary>
    public TResult Result { get; }

    /// <summary>
    /// Creates a continuation that executes when the target task completes.
    /// </summary>
    /// <typeparam name="TNewResult">The type of the result produced by the continuation.</typeparam>
    /// <param name="func">The function to execute when the task completes.</param>
    /// <returns>A new task representing the continuation.</returns>
    IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> func);
}