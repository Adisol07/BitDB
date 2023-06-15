using System;

namespace BitDB;

public class Document
{
    public string? ID { get; }
    public string? Name { get; }
    public List<Field>? Fields { get; set; }

    public Document(string name)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Name = name;
        this.Fields = new List<Field>();
    }
}