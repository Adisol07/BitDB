using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace BitDB;

public static class Client
{
    public static List<string> Flags = new List<string>();

    public static QueryResponse SendQuery(Query query, ConnectionArgs connection) => SendQuery(query, connection.Address, connection.Port, connection.Password);
    public static QueryResponse SendQuery(Query query, string ipaddress, int port, string password)
    {
        UdpClient? client = new UdpClient();
        client.Connect(ipaddress, port);
        if (Flags.Contains("logall") || Flags.Contains("logconnection"))
            Console.WriteLine("Connected to server: " + ipaddress + ":" + port.ToString());

        string json = JsonConvert.SerializeObject(query);
        client.Send(System.Text.Encoding.Unicode.GetBytes("#" + password + "#" + json));
        if (Flags.Contains("logall") || Flags.Contains("logsend"))
            Console.WriteLine("Sended: " + query.ID + " to server: " + ipaddress + ":" + port.ToString());

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.Unicode.GetString(receivedData);
        if (Flags.Contains("logall") || Flags.Contains("logreceived"))
            Console.WriteLine("Recieved: " + receivedMessage + " from server: " + ipaddress + ":" + port.ToString());

        return JsonConvert.DeserializeObject<QueryResponse>(receivedMessage);
    }

    public static Response SendCommand(string command, ConnectionArgs connection) => SendCommand(command, connection.Address, connection.Port, connection.Password);
    public static Response SendCommand(string command, string ipaddress, int port, string password)
    {
        UdpClient? client = new UdpClient();
        client.Connect(ipaddress, port);
        if (Flags.Contains("logall") || Flags.Contains("logconnection"))
            Console.WriteLine("Connected to server: " + ipaddress + ":" + port.ToString());

        client.Send(System.Text.Encoding.Unicode.GetBytes("#" + password + "#" + command));
        if (Flags.Contains("logall") || Flags.Contains("logsend"))
            Console.WriteLine("Sended: " + command + " to server: " + ipaddress + ":" + port.ToString());

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.Unicode.GetString(receivedData);
        if (Flags.Contains("logall") || Flags.Contains("logreceived"))
            Console.WriteLine("Recieved: " + receivedMessage + " from server: " + ipaddress + ":" + port.ToString());

        return JsonConvert.DeserializeObject<Response>(receivedMessage);
    }
}
public class ConnectionArgs
{
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 44;
    public string Password { get; set; } = "";

    public ConnectionArgs()
    { }
    public ConnectionArgs(string addr, int port, string password)
    {
        this.Address = addr;
        this.Port = port;
        this.Password = password;
    }
}