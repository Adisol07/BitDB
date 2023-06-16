using System;
using BitDB;

namespace BitDB.ServerApp;

internal class Program
{
    public static void Main(string[] args)
    {
    again:
        Console.Title = "Loading..";
        Server server = new Server("./db", 44);
        Console.Title = "[BitDB Server]";
        server.Start();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Press SPACE key to restart server\n");
        Console.Write("Press ANY OTHER key to exit\n");
        var key = Console.ReadKey().Key;
        if (key == ConsoleKey.Spacebar)
            goto again;
    }
}   