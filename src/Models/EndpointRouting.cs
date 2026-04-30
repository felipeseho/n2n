namespace n2n.Models;

public class EndpointRouting
{
    public string Column { get; set; } = string.Empty;
    public Dictionary<string, string> Map { get; set; } = new();
}
