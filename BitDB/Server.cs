using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

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
        Console.WriteLine("Server is loaded!"); 
        server = new UdpClient(Port);
        this.canListen = true;
        listen();
    } 
    public void LoadAll()
    {
        Databases.Clear();
        var files = Directory.GetFiles(DataFolder);
        foreach(var file in files)
        {
            if (file.EndsWith(".json") == false)
                continue;
            LoadDatabase(file);
        }
    }
    public void LoadDatabase(string dbFile)
    {
        if (File.Exists(dbFile) == false)
            throw new Exception("Database file does not exist");

        Database database = JsonConvert.DeserializeObject<Database>(File.ReadAllText(dbFile));
        Databases.Add(database);
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
                Console.WriteLine("[" + clientEndpoint.Address + ":" + clientEndpoint.Port + "]: Received message: " + receivedMessage);
                Response response = new Response("Could not fetch request"); 
                                                                                                        
                //Execute:  
                string[] words = receivedMessage.Split(' ');
                if (words[0] == "MAKE")
                {
                    //string pattern = "\"([a-z]+?)\"";
                    string pattern = "'(.*?)'";
                    if (words[1].ToLower() == "database")
                    {
                        MatchCollection matches = Regex.Matches(receivedMessage, pattern);

                        Database database = new Database(matches[0].Value.Replace("'", ""));
                        Databases.Add(database);

                        response = new Response("Successfully created database");

                        SaveData(DataFolder);
                        LoadAll();
                    }
                    else if (words[1].ToLower() == "document")
                    {
                        MatchCollection matches = Regex.Matches(receivedMessage, pattern);

                        string[] path = matches[0].Value.Replace("'", "").Split('/');
                        Document document = new Document(path[1]);
                        response = new Response("Could not find subitem");
                        int dbIndex = 0;
                        foreach(Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                Databases[dbIndex].Documents.Add(document);
                                response = new Response("Successfully created document");
                                SaveData(DataFolder);
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else if (words[1].ToLower() == "field")
                    {
                        MatchCollection matches = Regex.Matches(receivedMessage, pattern);

                        string[] path = matches[0].Value.Replace("'", "").Split('/');
                        Field field = new Field(path[2], null);
                        response = new Response("Could not find subitem");
                        int dbIndex = 0;
                        int docIndex = 0;
                        foreach(Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach(Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {   
                                        Databases[dbIndex].Documents[docIndex].Fields.Add(field);
                                        response = new Response("Successfuly created field");
                                        SaveData(DataFolder);
                                        break;
                                    }

                                    docIndex++;
                                }
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else
                    {
                        response = new Response("Invalid make type");
                    }
                }
                else
                {
                    response = new Response("Invalid command");
                }

                byte[] responseData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response));
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