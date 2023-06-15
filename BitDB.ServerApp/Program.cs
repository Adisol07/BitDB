using System;
using BitDB;

namespace BitDB.ServerApp;

internal class Program
{
    public static void Main(string[] args)
    {
        Server server = new Server("./db", 44);
        Console.Title = "BitDB.Server";
        server.Start();

        Console.ReadKey();
    }
}