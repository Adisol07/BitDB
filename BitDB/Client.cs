using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace BitDB;

public static class Client
{
    public static List<string> Flags = new List<string>();

    public static QueryResponse SendQuery(Query query, ConnectionArgs connection)
    {
        if (connection.encryptionKey == null || connection.GetEncryptionKeyEverytime)
            connection.GetEncryption();
        if (connection.CanConnect == false)
            return new QueryResponse("Client can't connect to server! ");
        UdpClient? client = new UdpClient();
        client.Connect(connection.Address, connection.Port);
        if (Flags.Contains("logall") || Flags.Contains("logconnection"))
            Console.WriteLine("Connected to server: " + connection.Address + ":" + connection.Port.ToString());

        string json = JsonConvert.SerializeObject(query);
        string plain = "#" + connection.Password + "#" + json;
        byte[] encrypted = null;
        using (Aes aes = Aes.Create())
        {
            aes.Key = connection.encryptionKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] dataBytes = Encoding.Unicode.GetBytes(plain);
            encrypted = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        }
        client.Send(encrypted);
        if (Flags.Contains("logall") || Flags.Contains("logsend"))
            Console.WriteLine("Sended: " + query.ID + " to server: " + connection.Address + ":" + connection.Port.ToString());

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.Unicode.GetString(receivedData);
        if (Flags.Contains("logall") || Flags.Contains("logreceived"))
            Console.WriteLine("Recieved: " + receivedMessage + " from server: " + connection.Address + ":" + connection.Port.ToString());

        return JsonConvert.DeserializeObject<QueryResponse>(receivedMessage);
    }
    public static QueryResponse SendQuery(Query query, string ipaddress, int port, string password) => SendQuery(query, new ConnectionArgs(ipaddress, port, password));

    public static Response SendCommand(string command, ConnectionArgs connection)
    {
        if (connection.encryptionKey == null || connection.GetEncryptionKeyEverytime)
            connection.GetEncryption();
        if (connection.CanConnect == false)
            return new Response("Client can't connect to server! ");
        UdpClient? client = new UdpClient();
        client.Connect(connection.Address, connection.Port);
        if (Flags.Contains("logall") || Flags.Contains("logconnection"))
            Console.WriteLine("Connected to server: " + connection.Address + ":" + connection.Port.ToString());

        string plain = "#" + connection.Password + "#" + command;
        byte[] encrypted = null;
        using (Aes aes = Aes.Create())
        {
            aes.Key = connection.encryptionKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] dataBytes = Encoding.Unicode.GetBytes(plain);
            encrypted = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        }
        client.Send(encrypted);
        if (Flags.Contains("logall") || Flags.Contains("logsend"))
            Console.WriteLine("Sended: " + command + " to server: " + connection.Address + ":" + connection.Port.ToString());

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.Unicode.GetString(receivedData);
        if (Flags.Contains("logall") || Flags.Contains("logreceived"))
            Console.WriteLine("Recieved: " + receivedMessage + " from server: " + connection.Address + ":" + connection.Port.ToString());

        return JsonConvert.DeserializeObject<Response>(receivedMessage);
    }
    public static Response SendCommand(string command, string ipaddress, int port, string password) => SendCommand(command, new ConnectionArgs(ipaddress, port, password));
}
public class ConnectionArgs
{
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 44;
    public string Password { get; set; } = "";
    public bool CanConnect { get; internal set; } = false;
    public bool GetEncryptionKeyEverytime { get; set; } = false;

    internal byte[] encryptionKey = null;

    public ConnectionArgs()
    { }
    public ConnectionArgs(string addr, int port, string password, bool getEncryptionKeyEverytime = false)
    {
        this.Address = addr;
        this.Port = port;
        this.Password = password;
        this.GetEncryptionKeyEverytime = getEncryptionKeyEverytime;
    }

    public void GetEncryption()
    {
        UdpClient? client = new UdpClient();
        client.Connect(Address, Port);
        string c = "GET-ENCRYPTIONKEY";
        client.Send(System.Text.Encoding.Unicode.GetBytes(c));
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.Unicode.GetString(receivedData);
        Response response = JsonConvert.DeserializeObject<Response>(receivedMessage);
        if (response.Message == "SUCCESS")
        {
            this.encryptionKey = Convert.FromBase64String(response.Tag);
            CanConnect = true;
        }
        else
        {
            CanConnect = false;
        }
    }
}