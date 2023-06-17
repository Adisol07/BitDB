using System;

namespace BitDB;

[Serializable]
public class Query
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public List<string> Commands { get; set; } = new List<string>();
    public DateTime Time { get; set; } = DateTime.Now;

    public Query(params string[] commands)
    {
        this.Commands = commands.ToList();
    }
}