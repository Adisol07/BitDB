using System;
using BitDB;
using Newtonsoft.Json;

namespace BitDB.ServerApp;

internal class Program
{
    public static void Main(string[] args)
    {
    again:
        Console.Title = "Loading..";
        ServerConfig config = new ServerConfig();

        if (File.Exists("./config.json"))
        {
            config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText("./config.json"));
        }
        else
        {
            Console.Write("Server name: ");
            var srvName = Console.ReadLine();
            Console.Write("Server password(none = disabled): ");
            var srvPassword = Console.ReadLine();
            Console.Write("Server port(def: 44): ");
            var srvPort = Console.ReadLine();
            Console.Write("Data folder(def: ./db): ");
            var dataFolder = Console.ReadLine();
            Console.Write("Max Request Lenght(def: 1024(1KB)): ");
            var maxRequestLenght = Console.ReadLine();

            config.Name = srvName;
            int port = 44;
            if (int.TryParse(srvPort, out port) && port > 0 && port < 65356)
                config.Port = port;
            else if (srvPort == "")
                config.Port = 44;
            else
            {
                Console.WriteLine("Invalid port!");
                goto again;
            }
            if (dataFolder == "")
                config.DataFolder = "./db";
            else
                config.DataFolder = dataFolder;
            config.Password = srvPassword;
            int mrl = 0;
            if (maxRequestLenght != "" && int.TryParse(maxRequestLenght, out mrl))
                config.MaxRequestLenght = mrl;
                

            File.WriteAllText("./config.json", JsonConvert.SerializeObject(config));
        }
        
        if (Directory.Exists(config.DataFolder) == false)
            Directory.CreateDirectory(config.DataFolder);

        Server server = new Server(config.DataFolder, config.Port, config.Password, config.MaxRequestLenght);
        Task handleKeys = new Task(() => { // Work in progress
            string command = "";
            while (true)
            {
                var key = Console.ReadKey().Key;
                if (key == ConsoleKey.Escape)
                {
                    server.Stop();
                    break;
                }
                else if (key == ConsoleKey.Enter)
                {
                    Console.WriteLine(server.HandleCommand(command).ToString());
                    command = "";
                }
                else
                {
                    command += ((char)key);
                }
            }
        });
        Console.Title = "[BitDB Server]: '" + config.Name + "'";
        handleKeys.Start();
        server.Start();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Press SPACE key to restart server\n");
        Console.Write("Press ANY OTHER key to exit\n");
        var key = Console.ReadKey().Key;
        if (key == ConsoleKey.Spacebar)
            goto again;
    }
}   