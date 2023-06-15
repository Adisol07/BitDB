using System;

namespace BitDB;

public class Field
{
    public string? Name { get; }
    public object? Value { get; set; }

    public Field()
    { }
    public Field(string name, object value)
    {
        this.Name = name;
        this.Value = value;
    }
}