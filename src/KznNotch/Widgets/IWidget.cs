namespace KznNotch.Widgets;

// WPF DataTemplates provide rendering; widgets own lifecycle and expose observable models.
public interface IWidget : IDisposable
{
    string Id { get; }
    string Name { get; }
    bool IsWide { get; }
    object Model { get; }
    void Initialize();
    void SetActive(bool active);
}

public abstract class Widget<T>(string id, string name, T model, bool wide = false) : IWidget where T : class
{
    public string Id => id;
    public string Name => name;
    public bool IsWide => wide;
    public T Model => model;
    object IWidget.Model => model;
    public virtual void Initialize() { }
    public virtual void SetActive(bool active) { }
    public virtual void Dispose() { }
}

public sealed class ClockWidget(Services.ClockService model) : Widget<Services.ClockService>("clock", "Uhr", model);
public sealed class FpsWidget(Services.FpsService model) : Widget<Services.FpsService>("fps", "Gaming & FPS", model, true);
public sealed class BatteryWidget(Services.BatteryService model) : Widget<Services.BatteryService>("battery", "Computerakku", model);
public sealed class MediaWidget(Services.MediaService model) : Widget<Services.MediaService>("media", "Medien", model, true);
public sealed class TimerWidget(Services.TimerService model) : Widget<Services.TimerService>("timer", "Timer & Fokus", model);
public sealed class SystemStatsWidget(Services.SystemStatsService model) : Widget<Services.SystemStatsService>("system", "System", model)
{
    public override void SetActive(bool active) => Model.SetActive(active);
}
public sealed class TodoWidget(Services.TodoService model) : Widget<Services.TodoService>("todo", "Aufgaben", model, true);
public sealed class ReminderWidget(Services.ReminderService model) : Widget<Services.ReminderService>("reminder", "Erinnerungen", model, true);
public sealed class BluetoothWidget(Services.BluetoothService model) : Widget<Services.BluetoothService>("bluetooth", "Bluetooth-Geräte", model, true);
public sealed class DiscordWidget(Services.DiscordService model) : Widget<Services.DiscordService>("discord", "Discord", model, true)
{
    public override void SetActive(bool active) => Model.SetActive(active);
}
