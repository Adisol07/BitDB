using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace BitDB;

public static class Client
{
    public static Response SendCommand(string command, ConnectionArgs connection) => SendCommand(command, connection.Address, connection.Port);
    public static Response SendCommand(string command, string ipaddress, int port)
    {
        UdpClient? client = new UdpClient();
        client.Connect(ipaddress, port);

        client.Send(System.Text.Encoding.UTF8.GetBytes(command));

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.ASCII.GetString(receivedData);

        return JsonConvert.DeserializeObject<Response>(receivedMessage);
    }
}
public class ConnectionArgs
{
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 44;

    public ConnectionArgs()
    { }
    public ConnectionArgs(string addr, int port)
    {
        this.Address = addr;
        this.Port = port;
    }
}