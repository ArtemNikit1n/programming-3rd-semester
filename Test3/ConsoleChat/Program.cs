// <copyright file="Program.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using ConsoleChat;

switch (args.Length)
{
    case 1 when int.TryParse(args[0], out var port):
    {
        if (port is >= 1 and <= 65535)
        {
            await ChatServer.RunAsync(port);
        }
        else
        {
            Console.WriteLine("Порт должен быть в диапазоне 1-65535");
        }

        break;
    }

    case 1:
        Console.WriteLine("Неверный формат порта. Укажите числовое значение");
        break;
    case 2:
    {
        var serverIp = args[0];

        if (int.TryParse(args[1], out var port))
        {
            if (port is >= 1 and <= 65535)
            {
                await ChatClient.RunAsync(serverIp, port);
            }
            else
            {
                Console.WriteLine("Порт должен быть в диапазоне 1-65535");
            }
        }
        else
        {
            Console.WriteLine("Неверный формат порта. Укажите числовое значение");
        }

        break;
    }

    default:
        ShowHelp();
        break;
}

return;

static void ShowHelp()
{
    Console.WriteLine("Консольный сетевой чат");
    Console.WriteLine("======================");
    Console.WriteLine("Использование:");
    Console.WriteLine("  Сервер: app.exe <порт>");
    Console.WriteLine("  Клиент: app.exe <IP-адрес> <порт>");
}