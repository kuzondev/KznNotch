using KznNotch.Storage;

namespace KznNotch.Services;

public sealed class TodoService : ObservableObject
{
    private readonly JsonStore _store;
    private string _newTitle = "";
    private int _newPriority;
    private DateTime? _newDue;
    public ObservableCollection<TodoItem> Items { get; }
    public string NewTitle { get => _newTitle; set { Set(ref _newTitle, value); CommandManager.InvalidateRequerySuggested(); } }
    public int NewPriority { get => _newPriority; set => Set(ref _newPriority, value); }
    public DateTime? NewDue { get => _newDue; set => Set(ref _newDue, value); }
    public string Summary => Items.Count == 0 ? "Platz für das, was zählt." : $"{Items.Count(x => !x.Completed)} offen · {Items.Count(x => x.Completed)} erledigt";
    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public TodoService(JsonStore store)
    {
        _store = store;
        Items = new(store.Load("todos.json", () => new List<TodoItem>()));
        foreach (var item in Items) item.PropertyChanged += ItemChanged;
        AddCommand = new RelayCommand(_ =>
        {
            var item = new TodoItem { Title = NewTitle.Trim(), Priority = NewPriority, DueDate = NewDue };
            item.PropertyChanged += ItemChanged; Items.Add(item); NewTitle = ""; NewDue = null; Save();
        }, _ => !string.IsNullOrWhiteSpace(NewTitle));
        DeleteCommand = new RelayCommand(p => { if (p is TodoItem item) { item.PropertyChanged -= ItemChanged; Items.Remove(item); Save(); } });
    }
    private void ItemChanged(object? sender, PropertyChangedEventArgs e) => Save();
    private void Save() { _store.Save("todos.json", Items.ToList()); Raise(nameof(Summary)); }
}
