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
        foreach (var file in files)
        {
            if (file.EndsWith(".json") && Path.GetFileNameWithoutExtension(file) != "")
            {
                LoadDatabase(file);
            }
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
        foreach (Database db in Databases)
        {
            string json = JsonConvert.SerializeObject(db);
            File.WriteAllText(dataFolder + "/" + db.ID + ".json", json);
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

                string[] words = receivedMessage.Split(' ');
                string pattern = "'(.*?)'";
                if (words[0] == "MAKE" || words[0] == "CREATE")
                {
                    if (words[1].ToLower() == "database")
                    {
                        MatchCollection matches = Regex.Matches(receivedMessage, pattern);

                        Database database = new Database(matches[0].Value.Replace("'", ""));
                        Databases.Add(database);

                        response = new Response("Successfully created database");

                        SaveData(DataFolder);
                    }
                    else if (words[1].ToLower() == "document")
                    {
                        MatchCollection matches = Regex.Matches(receivedMessage, pattern);

                        string[] path = matches[0].Value.Replace("'", "").Split('/');
                        Document document = new Document(path[1]);
                        response = new Response("Could not find subitem");
                        int dbIndex = 0;
                        foreach (Database db in Databases)
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
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach (Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {
                                        Databases[dbIndex].Documents[docIndex].Fields.Add(field);
                                        response = new Response("Successfully created field");
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
                else if (words[0] == "SET" || words[0] == "CHANGE" || words[0] == "INSERT")
                {
                    MatchCollection matches = Regex.Matches(receivedMessage, pattern);
                    response = new Response("Could not find subitem");
                    string[] path = matches[0].Value.Replace("'", "").Split('/');
                    string val = matches[1].Value.Replace("'", "");

                    if (path.Length == 1)
                    {
                        int dbIndex = 0;
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                if (words[1].ToLower() == "name")
                                {
                                    Databases[dbIndex].Name = val;
                                    response = new Response("Successfully renamed database");
                                }
                                else
                                {
                                    response = new Response("Invalid set type");
                                }
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else if (path.Length == 2)
                    {
                        int dbIndex = 0;
                        int docIndex = 0;
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach (Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {
                                        if (words[1].ToLower() == "name")
                                        {
                                            Databases[dbIndex].Documents[docIndex].Name = val;
                                            response = new Response("Successfully renamed document");
                                        }
                                        else
                                        {
                                            response = new Response("Invalid set type");
                                        }
                                        break;
                                    }

                                    docIndex++;
                                }
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else if (path.Length == 3)
                    {
                        int dbIndex = 0;
                        int docIndex = 0;
                        int fieldIndex = 0;
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach (Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {
                                        foreach (Field f in doc.Fields)
                                        {
                                            if (f.Name == path[2])
                                            {
                                                if (words[1].ToLower() == "name")
                                                {
                                                    Databases[dbIndex].Documents[docIndex].Fields[fieldIndex].Name = val;
                                                    response = new Response("Successfully renamed field");
                                                }
                                                else if (words[1].ToLower() == "value")
                                                {
                                                    Databases[dbIndex].Documents[docIndex].Fields[fieldIndex].Value = val;
                                                    response = new Response("Successfully changed field value");
                                                }
                                                else
                                                {
                                                    response = new Response("Invalid set type");
                                                }
                                                break;
                                            }

                                            fieldIndex++;
                                        }
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
                        response = new Response("Invalid path length");
                    }

                    SaveData(DataFolder);
                }
                else if (words[0] == "GET" || words[0] == "FETCH" || words[0] == "GRAB")
                {
                    MatchCollection matches = Regex.Matches(receivedMessage, pattern);
                    response = new Response("Could not find subitem");
                    string[] path = matches[0].Value.Replace("'", "").Split('/');

                    if (words[1].ToLower() == "namebyvalue")
                    {
                        response = new Response("Could not find field by value");
                        foreach (Database db in Databases)
                        {
                            foreach (Document doc in db.Documents)
                            {
                                foreach (Field f in doc.Fields)
                                {
                                    if (f.Value == matches[0].Value.Replace("'", ""))
                                    {
                                        response = new Response("Successfully fetched field name by value", f.Name);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (path.Length == 1 && (words[1] == "name" || words[1] == "value"))
                    {
                        int dbIndex = 0;
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                if (words[1].ToLower() == "name")
                                {
                                    response = new Response("Successfully fetched database name", db.Name);
                                }
                                else
                                {
                                    response = new Response("Invalid set type");
                                }
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else if (path.Length == 2 && (words[1] == "name" || words[1] == "value"))
                    {
                        int dbIndex = 0;
                        int docIndex = 0;
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach (Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {
                                        if (words[1].ToLower() == "name")
                                        {
                                            response = new Response("Successfully fetched document name", doc.Name);
                                        }
                                        else
                                        {
                                            response = new Response("Invalid set type");
                                        }
                                        break;
                                    }

                                    docIndex++;
                                }
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else if (path.Length == 3 && (words[1] == "name" || words[1] == "value"))
                    {
                        int dbIndex = 0;
                        int docIndex = 0;
                        int fieldIndex = 0;
                        foreach (Database db in Databases)
                        {
                            if (db.Name == path[0])
                            {
                                foreach (Document doc in db.Documents)
                                {
                                    if (doc.Name == path[1])
                                    {
                                        foreach (Field f in doc.Fields)
                                        {
                                            if (f.Name == path[2])
                                            {
                                                if (words[1].ToLower() == "name")
                                                {
                                                    response = new Response("Successfully fetched field name", f.Name);
                                                }
                                                else if (words[1].ToLower() == "value")
                                                {
                                                    response = new Response("Successfully fetched field value", f.Value);
                                                }
                                                else
                                                {
                                                    response = new Response("Invalid set type");
                                                }
                                                break;
                                            }

                                            fieldIndex++;
                                        }
                                        break;
                                    }

                                    docIndex++;
                                }
                                break;
                            }

                            dbIndex++;
                        }
                    }
                    else if (words[1] == "name" || words[1] == "value")
                    {
                        response = new Response("Invalid path length");
                    }
                }
                else

                {
                    response = new Response("Invalid command");
                }
                LoadAll();

                byte[] responseData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(response));
                server.Send(responseData, responseData.Length, clientEndpoint);
            }
        }
        catch (SocketException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            server?.Close();
        }
    }
}