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
        if (connection.serverEncryptionKey == null || connection.GetEncryptionKeyEverytime)
            connection.GetEncryption();
        if (connection.CanConnect == false)
            return new QueryResponse("Client can't connect to server! ");
        UdpClient? client = new UdpClient();
        client.Connect(connection.Address, connection.Port);
        if (Flags.Contains("logall") || Flags.Contains("logconnection"))
            Console.WriteLine("Connected to server: " + connection.Address + ":" + connection.Port.ToString());

        string json = JsonConvert.SerializeObject(query);
        string plain = "#" + JsonConvert.SerializeObject(connection.Authentification) + "#" + json;
        byte[] final = connection.Authentification.Encrypt(plain, connection.serverEncryptionKey);
        client.Send(final);
        if (Flags.Contains("logall") || Flags.Contains("logsend"))
            Console.WriteLine("Sended: " + query.ID + " to server: " + connection.Address + ":" + connection.Port.ToString());

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = connection.Authentification.Decrypt(receivedData, connection.serverEncryptionKey);
        if (Flags.Contains("logall") || Flags.Contains("logreceived"))
            Console.WriteLine("Recieved: " + receivedMessage + " from server: " + connection.Address + ":" + connection.Port.ToString());

        return JsonConvert.DeserializeObject<QueryResponse>(receivedMessage);
    }
    public static QueryResponse SendQuery(Query query, string ipaddress, int port, string user, string password) => SendQuery(query, new ConnectionArgs(ipaddress, port, new Auth(user, password)));

    public static Response SendCommand(string command, ConnectionArgs connection)
    {
        if (connection.serverEncryptionKey == null || connection.GetEncryptionKeyEverytime)
            connection.GetEncryption();
        if (connection.CanConnect == false)
            return new Response("Client can't connect to server! ");
        UdpClient? client = new UdpClient();
        client.Connect(connection.Address, connection.Port);
        if (Flags.Contains("logall") || Flags.Contains("logconnection"))
            Console.WriteLine("Connected to server: " + connection.Address + ":" + connection.Port.ToString());

        string plain = "#" + JsonConvert.SerializeObject(connection.Authentification) + "#" + command;
        byte[] encrypted = connection.Authentification.Encrypt(plain, connection.serverEncryptionKey);
        client.Send(encrypted);
        if (Flags.Contains("logall") || Flags.Contains("logsend"))
            Console.WriteLine("Sended: " + command + " to server: " + connection.Address + ":" + connection.Port.ToString());

        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = connection.Authentification.Decrypt(receivedData, connection.serverEncryptionKey);
        if (Flags.Contains("logall") || Flags.Contains("logreceived"))
            Console.WriteLine("Recieved: " + receivedMessage + " from server: " + connection.Address + ":" + connection.Port.ToString());

        return JsonConvert.DeserializeObject<Response>(receivedMessage);
    }
    public static Response SendCommand(string command, string ipaddress, int port, string user, string password) => SendCommand(command, new ConnectionArgs(ipaddress, port, new Auth(user, password)));
}
public class ConnectionArgs
{
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 44;
    public Auth Authentification { get; set; }
    public bool CanConnect { get; internal set; } = false;
    public bool GetEncryptionKeyEverytime { get; set; } = false;

    internal byte[] serverEncryptionKey;
    internal bool hasServerEncryptionKey = false;

    public ConnectionArgs()
    { }
    public ConnectionArgs(string address, int port, Auth authentification, bool getEncryptionKeyEverytime = false)
    {
        this.Address = address;
        this.Port = port;
        this.Authentification = authentification;
        this.GetEncryptionKeyEverytime = getEncryptionKeyEverytime;
    }
    public ConnectionArgs(string addr, int port, string user, string password, bool getEncryptionKeyEverytime = false)
    {
        this.Address = addr;
        this.Port = port;
        this.Authentification = new Auth(user, password);
        this.GetEncryptionKeyEverytime = getEncryptionKeyEverytime;
    }

    public void GetEncryption()
    {
        UdpClient? client = new UdpClient();
        client.Connect(Address, Port);
        string c = "GET-PUBLICKEY";
        client.Send(System.Text.Encoding.Unicode.GetBytes(c));
        IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.Receive(ref serverEndpoint);
        string receivedMessage = Encoding.Unicode.GetString(receivedData);
        Response response = JsonConvert.DeserializeObject<Response>(receivedMessage);
        if (response.Message == "SUCCESS")
        {
            this.serverEncryptionKey = Convert.FromBase64String(response.Tag);
            CanConnect = true;
        }
        else
        {
            CanConnect = false;
        }
    }
}
[Serializable]
public class Auth
{
    public string User { get; set; } = "admin";
    public string Password { get; set; } = "";

    public Auth()
    { }
    public Auth(string user, string password)
    {
        this.User = user;
        this.Password = password;
    }

    public byte[] Encrypt(string data, byte[] serverEncryptionKey)
    {
        byte[] encryptedData;
        using (Aes aes = Aes.Create())
        {
            aes.Key = serverEncryptionKey;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] dataBytes = Encoding.Unicode.GetBytes(data);
            encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
        }
        return encryptedData;
    }
    public string Decrypt(byte[] data, byte[] serverEncryptionKey)
    {
        string decryptedData;
        using (Aes aes = Aes.Create())
        {
            aes.Key = serverEncryptionKey;
            aes.Mode = CipherMode.ECB; 
            aes.Padding = PaddingMode.PKCS7;
            ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
            decryptedData = Encoding.Unicode.GetString(decryptedBytes);
        }
        return decryptedData;
    }
}