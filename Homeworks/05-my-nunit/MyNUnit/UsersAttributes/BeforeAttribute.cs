// <copyright file="BeforeAttribute.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnit.UsersAttributes;

/// <summary>
/// An attribute for marking methods that should be executed before each test in the class.
/// Is applied to the methods of the class instance.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BeforeAttribute : Attribute;
