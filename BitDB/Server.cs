using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace BitDB;

public class Server
{
    private UdpClient? server;
    private bool canListen = false;
    private byte[] encryptionKey;

    public List<Database>? Databases { get; set; }
    public int Port { get; }
    public string DataFolder { get; }
    public int MaxRequestLenght { get; set; }
    public bool ShowEncryptedMessage { get; set; } = false;
    public Dictionary<string, List<string>> Users { get; set; }

    public Server(string dataFolder = "./db", int port = 44, int maxRequestLenght = 1024)
    {
        this.DataFolder = dataFolder;
        this.Port = port;
        this.Databases = new List<Database>();
        this.Users = new Dictionary<string, List<string>>();
        this.MaxRequestLenght = maxRequestLenght;
        using (Aes aes = Aes.Create())
        {
            aes.GenerateKey();
            encryptionKey = aes.Key;
        }
        File.WriteAllText("./currentEncryptionKey.txt", Convert.ToBase64String(encryptionKey));
    }

    public void Start()
    {
        Console.WriteLine("Server is starting..");
        LoadAll();
        Console.WriteLine("Server is loaded!");
        server = new UdpClient(Port);
        this.canListen = true;
        Console.WriteLine("Server is listening for connections!");
        Console.WriteLine("Use client for handling commands");
        listen();
    }
    public void Stop()
    {
        Console.WriteLine("Server is stopping..");
        this.canListen = false;
        SaveData(DataFolder);
        Console.WriteLine("Server is saved!");
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
        Directory.Delete(dataFolder, true);
        Directory.CreateDirectory(dataFolder);
        foreach (Database db in Databases)
        {
            string json = JsonConvert.SerializeObject(db);
            File.WriteAllText(dataFolder + "/" + db.ID + ".json", json);
        }
    }
    public Response HandleCommand(string cmd, Auth currentUser)
    {
        Response response = new Response("Could not fetch request");
        string[] words = cmd.Split(' ');
        string pattern = "'(.*?)'";
        Console.WriteLine("Handling command: '" + cmd + "'");
        if (words[0] == "MAKE" || words[0] == "CREATE")
        {
            if (Users[currentUser.User + ";" + currentUser.Password].Contains("create") == false)
            {
                response = new Response("You don't have permission to create item!");
                return response;
            }

            if (words[1].ToLower() == "database" || words[1].ToLower() == "db")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);

                string name = matches[0].Value.Replace("'", "");
                if (name.Length > 100)
                {
                    response = new Response("Name of the database is too long (>100)");
                    return response;
                }
                else if (name.Length < 1)
                {
                    response = new Response("Name of the database is too short (<1)");
                    return response;
                }
                Database database = new Database(name);
                Databases.Add(database);

                response = new Response("Successfully created database");

                SaveData(DataFolder);
            }
            else if (words[1].ToLower() == "document" || words[1].ToLower() == "doc")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);

                string[] path = matches[0].Value.Replace("'", "").Split('/');
                string name = path[1];
                if (name.Length > 100)
                {
                    response = new Response("Name of the document is too long (>100)");
                    return response;
                }
                else if (name.Length < 1)
                {
                    response = new Response("Name of the document is too short (<1)");
                    return response;
                }
                Document document = new Document(name);
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
            else if (words[1].ToLower() == "field" || words[1].ToLower() == "f")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);

                string[] path = matches[0].Value.Replace("'", "").Split('/');
                string name = path[2];
                if (name.Length > 100)
                {
                    response = new Response("Name of the field is too long (>100)");
                    return response;
                }
                else if (name.Length < 1)
                {
                    response = new Response("Name of the field is too short (<1)");
                    return response;
                }
                string value = null;
                if (matches.Count == 2)
                {
                    value = matches[1].Value.Replace("'", "");
                    if (value.Length > MaxRequestLenght)
                    {
                        response = new Response("Value for new field is too long (>" + MaxRequestLenght + ") - Creation of field was denied");
                        return response;
                    }
                }
                Field field = new Field(name, value);
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
                    docIndex = 0;
                }
            }
            else
            {
                response = new Response("Invalid make type");
            }
        }
        else if (words[0] == "SET" || words[0] == "CHANGE" || words[0] == "INSERT")
        {
            if (Users[currentUser.User + ";" + currentUser.Password].Contains("set") == false)
            {
                response = new Response("You don't have permission to set item!");
                return response;
            }

            MatchCollection matches = Regex.Matches(cmd, pattern);
            response = new Response("Could not find subitem");
            string[] path = matches[0].Value.Replace("'", "").Split('/');
            string val = matches[1].Value.Replace("'", "");
            if (words[1].ToLower() == "name" && val.Length > 100)
            {
                response = new Response("New name is too long (>100)");
                return response;
            }
            else if (words[1].ToLower() == "name" && val.Length < 1)
            {
                response = new Response("New name is too short (<1)");
                return response;
            }
            else if (words[1].ToLower() == "value" && val.Length > MaxRequestLenght)
            {
                response = new Response("New value is too long (>" + MaxRequestLenght + ")");
                return response;
            }

            if (path.Length == 1)
            {
                int dbIndex = 0;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
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
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        foreach (Document doc in db.Documents)
                        {
                            if (doc.Name == path[1] || doc.ID == path[1])
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
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        foreach (Document doc in db.Documents)
                        {
                            if (doc.Name == path[1] || doc.ID == path[1])
                            {
                                foreach (Field f in doc.Fields)
                                {
                                    if (f.Name == path[2] || f.ID == path[2])
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
            if (Users[currentUser.User + ";" + currentUser.Password].Contains("get") == false)
            {
                response = new Response("You don't have permission to get item!");
                return response;
            }

            MatchCollection matches = Regex.Matches(cmd, pattern);
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

            if (path.Length == 1 && (words[1].ToLower() == "name" || words[1].ToLower() == "value" || words[1].ToLower() == "json" || words[1].ToLower() == "id"))
            {
                int dbIndex = 0;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        if (words[1].ToLower() == "name")
                        {
                            response = new Response("Successfully fetched database name", db.Name);
                        }
                        else if (words[1].ToLower() == "id")
                        {
                            response = new Response("Successfully fetched database ID", db.ID);
                        }
                        else if (words[1].ToLower() == "value" || words[1].ToLower() == "json")
                        {
                            response = new Response("Successfully fetched database json-value", JsonConvert.SerializeObject(db));
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
            else if (path.Length == 2 && (words[1].ToLower() == "name" || words[1].ToLower() == "value" || words[1].ToLower() == "json" || words[1].ToLower() == "id"))
            {
                int dbIndex = 0;
                int docIndex = 0;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        foreach (Document doc in db.Documents)
                        {
                            if (doc.Name == path[1] || doc.ID == path[1])
                            {
                                if (words[1].ToLower() == "name")
                                {
                                    response = new Response("Successfully fetched document name", doc.Name);
                                }
                                else if (words[1].ToLower() == "id")
                                {
                                    response = new Response("Successfully fetched document ID", doc.ID);
                                }
                                else if (words[1].ToLower() == "value" || words[1].ToLower() == "json")
                                {
                                    response = new Response("Successfully fetched document json-value", JsonConvert.SerializeObject(doc));
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
            else if (path.Length == 3 && (words[1].ToLower() == "name" || words[1].ToLower() == "value" || words[1].ToLower() == "json" || words[1].ToLower() == "id"))
            {
                int dbIndex = 0;
                int docIndex = 0;
                int fieldIndex = 0;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        foreach (Document doc in db.Documents)
                        {
                            if (doc.Name == path[1] || doc.ID == path[1])
                            {
                                foreach (Field f in doc.Fields)
                                {
                                    if (f.Name == path[2] || f.ID == path[2])
                                    {
                                        if (words[1].ToLower() == "name")
                                        {
                                            response = new Response("Successfully fetched field name", f.Name);
                                        }
                                        else if (words[1].ToLower() == "id")
                                        {
                                            response = new Response("Successfully fetched field ID", f.ID);
                                        }
                                        else if (words[1].ToLower() == "value")
                                        {
                                            response = new Response("Successfully fetched field value", f.Value);
                                        }
                                        else if (words[1].ToLower() == "json")
                                        {
                                            response = new Response("Successfully fetched field json-value", JsonConvert.SerializeObject(f));
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
                response = new Response("Invalid path length or argument");
            }
        }
        else if (words[0] == "DEL" || words[0] == "REM" || words[0] == "DELETE" || words[0] == "REMOVE")
        {
            if (Users[currentUser.User + ";" + currentUser.Password].Contains("delete") == false)
            {
                response = new Response("You don't have permission to delete item!");
                return response;
            }

            MatchCollection matches = Regex.Matches(cmd, pattern);
            response = new Response("Could not find subitem");
            string[] path = matches[0].Value.Replace("'", "").Split('/');

            if (path.Length == 1)
            {
                int dbIndex = 0;
                bool found = false;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        found = true;
                        break;
                    }
                    dbIndex++;
                }
                if (found)
                {
                    Databases.RemoveAt(dbIndex);
                    response = new Response("Successfully deleted database!");
                }
                else
                {
                    response = new Response("Item does not exist!");
                }
            }
            else if (path.Length == 2)
            {
                int dbIndex = 0;
                int docIndex = 0;
                bool found = false;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        foreach (Document doc in db.Documents)
                        {
                            if (doc.Name == path[1] || doc.ID == path[1])
                            {
                                found = true;
                                break;
                            }
                            docIndex++;
                        }
                        break;
                    }
                    dbIndex++;
                }
                if (found)
                {
                    Databases[dbIndex].Documents.RemoveAt(docIndex);
                    response = new Response("Successfully deleted document!");
                }
                else
                {
                    response = new Response("Item does not exist!");
                }
            }
            else if (path.Length == 3)
            {
                int dbIndex = 0;
                int docIndex = 0;
                int fieldIndex = 0;
                bool found = false;
                foreach (Database db in Databases)
                {
                    if (db.Name == path[0] || db.ID == path[0])
                    {
                        foreach (Document doc in db.Documents)
                        {
                            if (doc.Name == path[1] || doc.ID == path[1])
                            {
                                foreach (Field f in doc.Fields)
                                {
                                    if (f.Name == path[2] || f.ID == path[2])
                                    {
                                        found = true;
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
                if (found)
                {
                    Databases[dbIndex].Documents[docIndex].Fields.RemoveAt(fieldIndex);
                    response = new Response("Successfully deleted field!");
                }
                else
                {
                    response = new Response("Item does not exist!");
                }
            }
            else
            {
                response = new Response("Invalid path length");
            }

            SaveData(DataFolder);
        }
        else if (words[0] == "MANAGE")
        {
            if (Users[currentUser.User + ";" + currentUser.Password].Contains("manage") == false)
            {
                response = new Response("You don't have permission to manage this server!");
                return response;
            }

            if (words[1].ToLower() == "user")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);
                if (matches.Count == 0)
                {
                    response = new Response("Successfully fetched to json your permissions!", JsonConvert.SerializeObject(Users[currentUser.User + ";" + currentUser.Password]));
                }
                else
                {
                    string username = matches[0].Value.Replace("'", "");
                    string password = matches[1].Value.Replace("'", "");
                    response = new Response("Successfully fetched to json users permissions!", JsonConvert.SerializeObject(Users[username + ";" + password]));
                }
            }
            else if (words[1].ToLower() == "users" || words[1].ToLower() == "listusers")
            {
                response = new Response("Successfully fetched to json list of users!", JsonConvert.SerializeObject(Users));
            }
            else if (words[1].ToLower() == "adduser")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);
                string username = matches[0].Value.Replace("'", "");
                string password = matches[1].Value.Replace("'", "");
                string[] permissions = matches[2].Value.Replace("'", "").Split(";");
                Users.Add(username + ";" + password, permissions.ToList());
                File.WriteAllText("./users.json", JsonConvert.SerializeObject(Users));
                response = new Response("Successfully added new user!");
            }
            else if (words[1].ToLower() == "removeuser" || words[1].ToLower() == "remuser")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);
                string username = matches[0].Value.Replace("'", "");
                string password = matches[1].Value.Replace("'", "");
                Users.Remove(username + ";" + password);
                File.WriteAllText("./users.json", JsonConvert.SerializeObject(Users));
                response = new Response("Successfully removed user!");
            }
            else if (words[1].ToLower() == "addpermission" || words[1].ToLower() == "addperm")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);
                string username = matches[0].Value.Replace("'", "");
                string password = matches[1].Value.Replace("'", "");
                string[] permissions = matches[2].Value.Replace("'", "").Split(";");
                Users[username + ";" + password].AddRange(permissions);
                File.WriteAllText("./users.json", JsonConvert.SerializeObject(Users));
                response = new Response("Successfully added permissions to user!");
            }
            else if (words[1].ToLower() == "removepermission" || words[1].ToLower() == "remperm")
            {
                MatchCollection matches = Regex.Matches(cmd, pattern);
                string username = matches[0].Value.Replace("'", "");
                string password = matches[1].Value.Replace("'", "");
                string permission = matches[2].Value.Replace("'", "");
                Users[username + ";" + password].Remove(permission);
                File.WriteAllText("./users.json", JsonConvert.SerializeObject(Users));
                response = new Response("Successfully removed permission from user!");
            }
            else
            {
                response = new Response("Invalid argument");
            }
        }
        else
        {
            response = new Response("Invalid command");
        }
        LoadAll();
        return response;
    }

    private void listen()
    {
        while (this.canListen)
        {
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedData = server.Receive(ref clientEndpoint);
            if (ShowEncryptedMessage)
                Console.WriteLine("[" + clientEndpoint.Address + ":" + clientEndpoint.Port + "]: Received message: '" + Encoding.Unicode.GetString(receivedData) + "'");

            if (Encoding.Unicode.GetString(receivedData) == "GET-PUBLICKEY")
            {
                Console.WriteLine("Sending public encryption key to: '" + clientEndpoint.Address + ":" + clientEndpoint.Port + "'");
                Response response = new Response("SUCCESS", Convert.ToBase64String(encryptionKey));
                byte[] responseData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                server.Send(responseData, responseData.Length, clientEndpoint);
                continue;
            }

            Auth clientAuth = null;
            string finalMessage = "";
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decryptedBytes = decryptor.TransformFinalBlock(receivedData, 0, receivedData.Length);
                finalMessage = Encoding.Unicode.GetString(decryptedBytes);
            }
            if (finalMessage.StartsWith("#"))
            {
                MatchCollection matches = Regex.Matches(finalMessage, "#(.*?)#");
                clientAuth = JsonConvert.DeserializeObject<Auth>(matches[0].Value.Replace("#", ""));
                finalMessage = finalMessage.Remove(0, matches[0].Value.Length);
            }      
            Console.WriteLine("[" + clientEndpoint.Address + ":" + clientEndpoint.Port + "(" + clientAuth.User + ")]: Received" + (ShowEncryptedMessage ? " decrypted" : "") + " message: '" + finalMessage + "'");
            try
            {
                if (finalMessage.StartsWith("{") && finalMessage.EndsWith("}"))
                {
                    QueryResponse response = new QueryResponse("Invalid query");

                    Query query = JsonConvert.DeserializeObject<Query>(finalMessage);
                    if (Users.ContainsKey(clientAuth.User + ";" + clientAuth.Password))
                    {
                        Console.WriteLine("Fetching query: '" + query.ID + "'");
                        int fetched = 0;
                        foreach (string command in query.Commands)
                        {
                            Response r = HandleCommand(command, clientAuth);
                            fetched++;
                            response.Responses.Add(r);
                        }
                        response.Message = "Fetched commands: " + fetched;
                    }
                    else
                    {
                        Console.WriteLine("Request: '" + query.ID + "' was denied because of invalid client auth!");
                        response.Message = "Invalid client auth";
                    }

                    response.RequestDuration = query.Time - DateTime.Now;
                    //byte[] responseData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                    byte[] responseData = null;
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = encryptionKey;
                        aes.Mode = CipherMode.ECB;
                        aes.Padding = PaddingMode.PKCS7;
                        ICryptoTransform encryptor = aes.CreateEncryptor();
                        byte[] dataBytes = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                        responseData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    }
                    server.Send(responseData, responseData.Length, clientEndpoint);
                }
                else
                {
                    Response response = new Response("Could not fetch request");

                    if (Users.ContainsKey(clientAuth.User + ";" + clientAuth.Password))
                    {
                        Console.WriteLine("Fetching command: '" + finalMessage + "'");
                        response = HandleCommand(finalMessage, clientAuth);
                    }
                    else
                    {
                        Console.WriteLine("Command: '" + finalMessage + "' was denied because of invalid client auth!");
                        response.Message = "Invalid client auth";
                    }

                    //byte[] responseData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                    byte[] responseData = null;
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = encryptionKey;
                        aes.Mode = CipherMode.ECB;
                        aes.Padding = PaddingMode.PKCS7;
                        ICryptoTransform encryptor = aes.CreateEncryptor();
                        byte[] dataBytes = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                        responseData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    }
                    server.Send(responseData, responseData.Length, clientEndpoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[LISTEN_Error]: '" + ex.Message + "'");

                if (finalMessage.StartsWith("{") && finalMessage.EndsWith("}"))
                {
                    QueryResponse response = new QueryResponse("An error occurred while fetching your request");
                    //byte[] responseData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                    byte[] responseData = null;
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = encryptionKey;
                        aes.Mode = CipherMode.ECB;
                        aes.Padding = PaddingMode.PKCS7;
                        ICryptoTransform encryptor = aes.CreateEncryptor();
                        byte[] dataBytes = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                        responseData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    }
                    server.Send(responseData, responseData.Length, clientEndpoint);
                }
                else
                {
                    Response response = new Response("An error occurred while fetching your request");
                    //byte[] responseData = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                    byte[] responseData = null;
                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = encryptionKey;
                        aes.Mode = CipherMode.ECB;
                        aes.Padding = PaddingMode.PKCS7;
                        ICryptoTransform encryptor = aes.CreateEncryptor();
                        byte[] dataBytes = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(response));
                        responseData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                    }
                    server.Send(responseData, responseData.Length, clientEndpoint);
                }
            }
        }
    }
}