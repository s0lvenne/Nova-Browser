#nullable enable
namespace NovaBrowser.Models;

public class BookmarkItem
{
    public string Title  { get; set; } = "";
    public string Url    { get; set; } = "";
    public string Folder { get; set; } = "";
    public string Icon   { get; set; } = "";
}
