// <copyright file="ChatServer.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace ConsoleChat;

using System.Net;
using System.Net.Sockets;
using System.Text;


/// <summary>
/// Represents a chat server.
/// </summary>
public static class ChatServer
{
    /// <summary>
    /// Starting the server.
    /// </summary>
    /// <param name="port">Starting the server.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task RunAsync(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, port);

            listener.Start();
            Console.WriteLine($"Сервер запущен на порту {port}");
            Console.WriteLine("Ожидание подключения клиента...");

            var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Клиент подключен!");
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

            listener.Stop();
            Console.WriteLine("Сервер остановлен");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Сетевая ошибка: {ex.Message}");
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
                    Console.WriteLine("\nКлиент отключился");
                    break;
                }

                if (message.ToLower() == "exit")
                {
                    Console.WriteLine("\nКлиент завершил соединение");
                    break;
                }

                Console.WriteLine($"Клиент: {message}");
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"\nОшибка при чтении от клиента: {ex.Message}");
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
                    Console.WriteLine("Завершение работы сервера...");
                    break;
                }

                await writer.WriteLineAsync(input);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"\nОшибка при отправке клиенту: {ex.Message}");
        }
    }
}