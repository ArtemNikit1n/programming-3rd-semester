// <copyright file="UploadRequest.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MyNUnitWeb.ModelsWeb;

/// <summary>
/// Model for receiving files from a client.
/// </summary>
public class UploadRequest
{
    /// <summary>
    /// Gets or sets uploadable file.
    /// </summary>
    public IFormFile? File { get; set; }
}