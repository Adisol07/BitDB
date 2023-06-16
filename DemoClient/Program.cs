using System;
using BitDB;

namespace DemoClient;

internal class Program
{
    public static void Main(string[] args)
    {
        ConnectionArgs connection = new ConnectionArgs("209.38.192.39", 44);
        Console.WriteLine(Client.SendCommand("GET value 'test/testovacidoc/fff'", connection).ToString());
        Console.ReadLine();
    }
}