#nullable enable
namespace NovaBrowser.Models;

public class SavedAccountItem
{
    public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
    public string Service { get; set; } = "Google";
    public string Login { get; set; } = "";
    public string AccountName
    {
        get => Login;
        set => Login = value;
    }
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string AddedDate { get; set; } = "";
    public System.DateTime ConnectedAt
    {
        get => System.DateTime.TryParse(AddedDate, out var dt) ? dt : System.DateTime.Now;
        set => AddedDate = value.ToString("yyyy-MM-dd HH:mm");
    }
}
