// <copyright file="ILazy.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace Lazy;

/// <summary>
/// Returns the calculated value.
/// On the first call, initiates the calculation.
/// On subsequent calls, returns the previously calculated value.
/// </summary>
/// <typeparam name="T">The type of the value that will be lazily initialized.</typeparam>
public interface ILazy<out T>
{
    /// <summary>
    /// Gets the lazily initialized value of type <typeparamref name="T"/>.
    /// </summary>
    /// <returns>
    /// The initialized value of type <typeparamref name="T"/>. Subsequent calls to this method
    /// will return the same instance without re-invoking the supplier function.
    /// </returns>
    T? Get();
}