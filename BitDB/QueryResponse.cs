using System;

namespace BitDB;

[Serializable]
public class QueryResponse
{
    public string? ID { get; set; } = Guid.NewGuid().ToString();
    public string? Message { get; set; }
    public List<Response> Responses { get; set; } = new List<Response>();
    public TimeSpan RequestDuration { get; set; } = TimeSpan.Zero;

    public QueryResponse(string message = "")
    {
        this.Message = message;
    }

    public override string ToString()
    {
        string final = "";

        final += "ID: " + ID + "\n";
        final += "Message: " + Message + "\n";
        final += "Duration: " + RequestDuration.Duration() + "\n";
        if (Responses.Count > 0)
        {
            final += "Responses: \n";
            foreach (Response res in Responses)
            {
                final += "  " + res.ID + ":\n";
                final += "      Message: " + res.Message + "\n";
                final += "      Tag: " + res.Tag + "\n";
            }
        }

        return final.Trim();
    }
}