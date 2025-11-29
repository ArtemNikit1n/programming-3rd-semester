// <copyright file="LazySingleThreaded.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy;

using System.Diagnostics;

/// <summary>
/// Provides a simple, non-thread-safe implementation of the <see cref="ILazy{T}"/> interface.
/// This implementation is intended for use in single-threaded scenarios only.
/// </summary>
/// <typeparam name="T">The type of the value that is being lazily initialized.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="LazySingleThreaded{T}"/> class.
/// </remarks>
/// <param name="supplier">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
/// <exception cref="ArgumentNullException">Thrown when the <paramref name="supplier"/> is <see langword="null"/>.</exception>
public class LazySingleThreaded<T>(Func<T> supplier) : ILazy<T>
{
    private Func<T>? supplier = supplier ?? throw new ArgumentNullException(nameof(supplier));
    private T? value;
    private bool isValueCreated;

    /// <inheritdoc/>
    /// <remarks>
    /// If <see cref="Get"/> is called for the first time, this method will execute the supplier delegate,
    /// store the result, and then return it. Subsequent calls will return the stored value without
    /// re-executing the supplier.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the supplier delegate was set to <see langword="null"/> internally before
    /// the value could be initialized. This indicates a severe initialization error and should not
    /// occur under normal circumstances.
    /// </exception>
    public T? Get()
    {
        if (this.isValueCreated || this.supplier == null)
        {
            return this.value;
        }

        this.value = this.supplier();
        this.isValueCreated = true;
        this.supplier = null;

        return this.value;
    }
}