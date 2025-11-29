// <copyright file="LazyMultiThreaded.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy;

/// <summary>
/// Provides a thread-safe implementation of the <see cref="ILazy{T}"/> interface.
/// This implementation ensures that the supplier delegate is executed at most once,
/// even when accessed from multiple threads simultaneously.
/// </summary>
/// <typeparam name="T">The type of the value that is being lazily initialized.</typeparam>
public class LazyMultiThreaded<T>(Func<T> supplier) : ILazy<T>
{
    private readonly Lock lockObject = new();
    private Func<T>? supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
    private T? value;
    private volatile bool isValueCreated;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// This method is thread-safe and guarantees that the supplier delegate is executed
    /// at most once, regardless of how many threads call this method simultaneously.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the supplier delegate was set to <see langword="null"/> internally before
    /// the value could be initialized. This indicates a severe initialization error and should not
    /// occur under normal circumstances.
    /// </exception>
    public T? Get()
    {
        if (this.isValueCreated)
        {
            return this.value;
        }

        lock (this.lockObject)
        {
            if (this.isValueCreated || this.supplier == null)
            {
                return this.value;
            }

            this.value = this.supplier();
            this.isValueCreated = true;
            this.supplier = null;
        }

        return this.value;
    }
}