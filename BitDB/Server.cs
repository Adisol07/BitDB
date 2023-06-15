using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace BitDB;

public class Server
{
    private UdpClient? server;
    private bool canListen = false;

    public List<Database>? Databases { get; set; }
    public int Port { get; }
    public string DataFolder { get; }

    public Server(string dataFolder = "./db", int port = 44)
    {
        this.DataFolder = dataFolder;
        this.Port = port;
        this.Databases = new List<Database>();
    }

    public void Start()
    {
        LoadAll();
        server = new UdpClient(Port);
        this.canListen = true;
        listen();
    }
    public void LoadAll()
    {
        var files = Directory.GetFiles(DataFolder);
        foreach(var file in files)
        {
            LoadDatabase(file);
        }
    }
    public void LoadDatabase(string dbFile)
    {
        if (File.Exists(dbFile) == false)
            throw new Exception("Database file does not exist");

        Databases.Add(JsonConvert.DeserializeObject<Database>(dbFile));
    }
    public void SaveData(string dataFolder)
    {
        foreach(Database db in Databases)
        {
            File.WriteAllText(dataFolder + "/" + db.ID + ".json", JsonConvert.SerializeObject(db));
        }
    }

    private void listen()
    {
        try
        {
            while (this.canListen)
            {
                IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receivedData = server.Receive(ref clientEndpoint);
                string receivedMessage = Encoding.ASCII.GetString(receivedData);

                //Execute:
                string[] words = receivedMessage.Split(' ');
                if (words[0] == "MAKE")
                {
                    if (words[1].ToLower() == "database")
                    {
                        int start = receivedMessage.IndexOf('"') + 1;
                        int end = receivedMessage.LastIndexOf('"', start);
                        string result = receivedMessage.Substring(start, end - start);

                        Database database = new Database(result);
                        Databases.Add(database);

                        SaveData(DataFolder);
                    }
                    else if (words[1].ToLower() == "document")
                    {
                        int start = receivedMessage.IndexOf('"') + 1;
                        int end = receivedMessage.LastIndexOf('"', start);
                        string result = receivedMessage.Substring(start, end - start);

                        string[] path = result.Split('/');
                        Document document = new Document(path[1]);
                        foreach(Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                db.Documents.Add(document);
                                break;
                            }
                        }
                        SaveData(DataFolder);
                    }
                    else if (words[1].ToLower() == "field")
                    {
                        int start = receivedMessage.IndexOf('"') + 1;
                        int end = receivedMessage.LastIndexOf('"', start);
                        string result = receivedMessage.Substring(start, end - start);

                        string[] path = result.Split('/');
                        Field field = new Field(path[2], null);
                        foreach(Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach(Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {
                                        doc.Fields.Add(field);
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        SaveData(DataFolder);
                    }
                }

                string responseMessage = "";
                byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
                server.Send(responseData, responseData.Length, clientEndpoint);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            server?.Close();
        }
    }
}