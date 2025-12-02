// <copyright file="TestAttribute.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.UsersAttributes;

/// <summary>
/// An attribute for marking methods as test methods.
/// Is applied to methods to indicate that they are tests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute(string? ignore, Type? expectedException) : Attribute
{
    /// <summary>
    /// Gets the type of expected exception.
    /// If specified, the test is considered successful only if an exception to this type is thrown.
    /// </summary>
    public Type? ExpectedException { get; } = expectedException;

    /// <summary>
    /// Gets the reason for ignoring the test.
    /// If there is no empty line, the test will be skipped with the specified reason.
    /// </summary>
    public string? Ignore { get; } = ignore;
}