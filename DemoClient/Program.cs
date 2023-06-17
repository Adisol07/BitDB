using System;
using BitDB;

namespace DemoClient;

internal class Program
{
    public static void Main(string[] args)
    {
        ConnectionArgs connection = new ConnectionArgs("127.0.0.1", 44, new Auth("admin", "1234"));
    again:
        Console.Write("Command: ");
        var command = Console.ReadLine();
        Console.WriteLine(Client.SendQuery(new Query(command), connection).ToString());
        goto again;
    }
}