#nullable enable
using System;

namespace NovaBrowser.Models;

public class HistoryItem
{
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime VisitedAt { get; set; } = DateTime.Now;
}
