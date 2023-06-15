using System;
using Newtonsoft.Json;

namespace BitDB;

public class Database
{
    public string? ID { get; }
    public string? Name { get; set; }
    public List<Document>? Documents { get; set; } = new List<Document>();

    public Database()
    { }
    public Database(string name)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Name = name;
    }
}