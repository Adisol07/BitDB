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
        Dictionary<string, List<string>> users = new Dictionary<string, List<string>>();

        if (File.Exists("./config.json") && File.Exists("./users.json"))
        {
            config = JsonConvert.DeserializeObject<ServerConfig>(File.ReadAllText("./config.json"));
            users = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText("./users.json"));
        }
        else
        {
            Console.Write("Server name: ");
            var srvName = Console.ReadLine();
            Console.Write("Admin password(none = disabled): ");
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
            int mrl = 0;
            if (maxRequestLenght != "" && int.TryParse(maxRequestLenght, out mrl))
                config.MaxRequestLenght = mrl;
                
            users.Add("admin;" + srvPassword, new List<string>() { "get", "set", "delete", "create", "manage" });

            File.WriteAllText("./users.json", JsonConvert.SerializeObject(users));
            File.WriteAllText("./config.json", JsonConvert.SerializeObject(config));
        }
        
        if (Directory.Exists(config.DataFolder) == false)
            Directory.CreateDirectory(config.DataFolder);

        Server server = new Server(config.DataFolder, config.Port, config.MaxRequestLenght);
        server.Users = users;
        Console.Title = "[BitDB Server]: '" + config.Name + "'";
        server.Start();

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Press SPACE key to restart server\n");
        Console.Write("Press ANY OTHER key to exit\n");
        var key = Console.ReadKey().Key;
        if (key == ConsoleKey.Spacebar)
            goto again;
    }
}   