namespace KznNotch.Models;

public sealed class TodoItem : ObservableObject
{
    private bool _completed;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public bool Completed { get => _completed; set => Set(ref _completed, value); }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public string Detail => $"{(Priority == 2 ? "Hohe Priorität" : Priority == 1 ? "Mittlere Priorität" : "")}{(DueDate is { } date ? $" · {date:dd.MM.}" : "")}".Trim(' ', '·');
}

public sealed class ReminderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public DateTimeOffset DueAt { get; set; }
    public bool Fired { get; set; }
    public string DueLabel => DueAt.ToLocalTime().ToString("dd.MM. · HH:mm");
}
