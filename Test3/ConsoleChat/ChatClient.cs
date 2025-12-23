// <copyright file="ChatClient.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace ConsoleChat;

using System.Net.Sockets;
using System.Text;

/// <summary>
/// Represents a chat client that can connect to a server and communicate in real-time.
/// </summary>
public static class ChatClient
{
    /// <summary>
    /// Asynchronously connects to the specified server and port.
    /// </summary>
    /// <param name="serverIp">The IP address or hostname of the server to connect to.</param>
    /// <param name="port">The port number to connect to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task RunAsync(string serverIp, int port)
    {
        if (serverIp == null)
        {
            throw new ArgumentNullException(nameof(serverIp));
        }

        try
        {
            Console.WriteLine($"Подключение к серверу {serverIp}:{port}...");
            var client = new TcpClient();

            await client.ConnectAsync(serverIp, port);
            Console.WriteLine("Подключение установлено!");
            Console.WriteLine("Введите сообщение или 'exit' для выхода");

            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            await using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.AutoFlush = true;
                var receiveTask = ReceiveMessagesAsync(reader);

                var sendTask = SendMessagesAsync(writer);

                await Task.WhenAny(receiveTask, sendTask);

                client.Close();
            }

            Console.WriteLine("Клиент остановлен");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Сетевая ошибка: {ex.Message}");
            Console.WriteLine("Возможно, сервер недоступен или указан неверный адрес/порт");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка ввода-вывода: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
        }
    }

    private static async Task ReceiveMessagesAsync(StreamReader reader)
    {
        try
        {
            while (true)
            {
                var message = await reader.ReadLineAsync();

                if (message == null)
                {
                    Console.WriteLine("\nСервер отключился");
                    break;
                }

                if (message.ToLower() == "exit")
                {
                    Console.WriteLine("\nСервер завершил соединение");
                    break;
                }

                Console.WriteLine($"Сервер: {message}");
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"\nОшибка при чтении от сервера: {ex.Message}");
        }
    }

    private static async Task SendMessagesAsync(StreamWriter writer)
    {
        try
        {
            while (true)
            {
                var input = Console.ReadLine();

                if (input != null && input.ToLower() == "exit")
                {
                    await writer.WriteLineAsync(input);
                    Console.WriteLine("Завершение работы клиента...");
                    break;
                }

                await writer.WriteLineAsync(input);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"\nОшибка при отправке серверу: {ex.Message}");
        }
    }
}