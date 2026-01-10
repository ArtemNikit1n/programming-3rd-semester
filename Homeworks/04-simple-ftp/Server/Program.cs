// <copyright file="Program.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

var port = 8080;
switch (args.Length)
{
    case 1 when int.TryParse(args[0], out var customPort):
        if (customPort is < 1024 or > 49151)
        {
            Console.WriteLine("Error: Incorrect port number.");
            Console.WriteLine("Usage: Enter a port between 1024 and 49151.");
            return;
        }

        port = customPort;
        break;
    case > 0:
        Console.WriteLine("Error: Incorrect arguments.");
        Console.WriteLine("Usage: Enter a port between 1024 and 49151.");
        return;
}

var server = new Server.Server(port);
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("Stopping server...");
    server.Dispose();
};

try
{
    await server.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Server error: {ex.Message}");
}