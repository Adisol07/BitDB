using System;

namespace BitDB;

public class Response
{
    public string? ID { get; }
    public string? Message { get; set; }
    public string? Tag { get; set; }

    public Response(string message, string tag = null)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Message = message;
        this.Tag = tag;
    }

    public override string ToString()
    {
        return "ID: " + ID + "\nMessage: " + Message + "\nTag: " + Tag;
    }
}