// <copyright file="IntegrationTests.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace ConsoleChat.Test;

using System.Text;

#pragma warning disable SA1600
public class IntegrationTests
{
    [Test]
    public async Task ServerAndClient_CanExchangeMessages()
    {
        const string serverMessage = "Привет от сервера";
        const string clientMessage = "Привет от клиента";

        using var serverStream = new MemoryStream();
        using var clientStream = new MemoryStream();

        var serverReader = new StreamReader(serverStream, Encoding.UTF8);
        var serverWriter = new StreamWriter(serverStream, Encoding.UTF8);
        serverWriter.AutoFlush = true;

        var clientReader = new StreamReader(clientStream, Encoding.UTF8);
        var clientWriter = new StreamWriter(clientStream, Encoding.UTF8);
        clientWriter.AutoFlush = true;

        await serverWriter.WriteLineAsync(clientMessage);
        serverStream.Position = 0;
        var receivedByServer = await serverReader.ReadLineAsync();
        Assert.That(receivedByServer, Is.EqualTo(clientMessage));

        await clientWriter.WriteLineAsync(serverMessage);
        clientStream.Position = 0;
        var receivedByClient = await clientReader.ReadLineAsync();
        Assert.That(receivedByClient, Is.EqualTo(serverMessage));
    }
}