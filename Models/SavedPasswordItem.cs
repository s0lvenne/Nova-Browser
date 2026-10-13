#nullable enable
namespace NovaBrowser.Models;

public class SavedPasswordItem
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Site { get; set; } = "";
    public string Login { get; set; } = "";
    public string Username
    {
        get => Login;
        set => Login = value;
    }
    public string Password { get; set; } = "";
    public string Date { get; set; } = "";
    public System.DateTime SavedAt
    {
        get => System.DateTime.TryParse(Date, out var dt) ? dt : System.DateTime.Now;
        set => Date = value.ToString("yyyy-MM-dd HH:mm");
    }
}
