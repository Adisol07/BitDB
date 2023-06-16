using System;

namespace BitDB;

[Serializable]
public class Document
{
    public string ID { get; set; }
    public string Name { get; set; }
    public List<Field> Fields { get; set; } = new List<Field>();

    public Document()
    { }
    public Document(string name)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Name = name;
    }
}