using System;

namespace BitDB;

[Serializable]
public class Field
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }

    public Field()
    { }
    public Field(string name, string value)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Name = name;
        this.Value = value;
    }
}