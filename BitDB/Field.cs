using System;

namespace BitDB;

[Serializable]
public class Field
{
    public string Name { get; set; }
    public string Value { get; set; }

    public Field()
    { }
    public Field(string name, string value)
    {
        this.Name = name;
        this.Value = value;
    }
}