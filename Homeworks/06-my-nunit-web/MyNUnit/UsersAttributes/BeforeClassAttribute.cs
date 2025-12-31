// <copyright file="BeforeClassAttribute.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.UsersAttributes;

/// <summary>
/// An attribute for marking methods that should be executed once after all tests in the class.
/// Applies to static methods of the class.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BeforeClassAttribute : Attribute;
