using System;
using Newtonsoft.Json;

namespace BitDB;

public class Database
{
    public string? ID { get; }
    public string? Name { get; set; }
    public List<Document>? Documents { get; set; }

    public Database(string name)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Name = name;
        this.Documents = new List<Document>();
    }
}