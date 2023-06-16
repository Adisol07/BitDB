using System;
using BitDB;

namespace DemoClient;

internal class Program
{
    public static void Main(string[] args)
    {
        ConnectionArgs connection = new ConnectionArgs("127.0.0.1", 44);
        Console.WriteLine(Client.SendCommand("GET namebyvalue 'hustyyy'", connection).ToString());
        Console.ReadLine();
    }
}