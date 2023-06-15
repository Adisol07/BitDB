using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace BitDB;

public class Client
{
    private UdpClient? client;

    public Client()
    { 
        client = new UdpClient();
    }

    public Response SendCommand(string command, string ipaddress, int port)
    {
        client.Connect(ipaddress, port);

        client.Send(System.Text.Encoding.UTF8.GetBytes(command));

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.ASCII.GetString(receivedData);

        return JsonConvert.DeserializeObject<Response>(receivedMessage);
    }
}