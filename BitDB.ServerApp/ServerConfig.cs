using System;

namespace BitDB.ServerApp;

[Serializable]
public class ServerConfig
{
    public string Name { get; set; } = "BitDB Default Server";
    public int Port { get; set; } = 44;
    public string DataFolder { get; set; } = "./";
    public int MaxRequestLenght { get; set; } = 1024;
}