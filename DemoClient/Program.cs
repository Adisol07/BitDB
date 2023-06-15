using System;
using BitDB;

namespace DemoClient;

internal class Program
{
    public static void Main(string[] args)
    {
        Client client = new Client();
        Console.WriteLine(client.SendCommand("MAKE document 'test/testik'", "127.0.0.1", 44).Message);
        Console.ReadLine();
    }
}