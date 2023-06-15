using System;

namespace BitDB;

public class Response
{
    public string? ID { get; }
    public string? Message { get; set; }
    public object? Tag { get; set; }

    public Response(string message, object tag = null)
    {
        this.ID = Guid.NewGuid().ToString();
        this.Message = message;
        this.Tag = tag;
    }
}