using System;

namespace BitDB;

public class Document
{
    public string? ID { get; }
    public string? Name { get; }
    public List<Field>? Fields { get; set; } = new List<Field>();

    public Document()
    { }
    public Document(string name)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Name = name;
    }
}