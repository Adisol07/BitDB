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
    { }

    public Response SendCommand(string command, string ipaddress, int port)
    {
        client.Connect(ipaddress, port);

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.ASCII.GetString(receivedData);

        return JsonConvert.DeserializeObject<Response>(receivedMessage);
    }
}