# Architektur

WPF wurde gewählt, weil `AllowsTransparency`, transparente Hit-Test-Flächen, HWND-Stile und ein Tray in einer nicht paketierten Desktopanwendung gut zusammenarbeiten. Ein späterer Wechsel der Rendering-Schicht ist durch die Trennung der Services möglich. .NET 10 ist die Basis für die Windows-Runtime-Projektionen.

```text
src/KznNotch/
  Core/          Composition Root, ObservableObject, Commands, UI-Dispatcher, Log
  Models/        Einstellungen, Aufgaben, Erinnerungen, Bluetooth-Geräte
  Services/      Windows-Anbindungen und Anwendungslogik
  Integrations/  Spotify-Adapter und austauschbare Windows-Benachrichtigungsquelle für Discord
  Storage/       atomare JSON-Speicherung und Fehlerwiederherstellung
  Animations/    zentrales Timing, Easing und Inhaltsübergänge
  Widgets/       Widget-Vertrag, Registrierung und Lifecycle
  ViewModels/    Notch-Zustand, Compact-Inhalte und Settings-Entwurf
  Views/         WPF-Fenster, Ressourcen und DataTemplates
  Utilities/     schmale Win32-Interop-Schicht
tests/KznNotch.Smoke/
```

`AppHost` besitzt die gemeinsamen Services. `WindowService` erstellt pro Zielmonitor ein `NotchWindow`. Jedes Fenster besitzt einen eigenen `NotchViewModel` und berechnet seine Widgetbreiten aus dem tatsächlich sichtbaren Viewport. Die Services bleiben monitorübergreifend geteilt.

`NotchViewModel` steuert Compact/Hover/Expanded. Hover aktiviert das Fenster nicht. Erst die bewusste Dashboard-Interaktion erlaubt Tastatureingaben. Windows-Nachrichten für Hit-Tests lassen ungenutzte transparente Flächen durch. Die feste transparente Fensterhülle verhindert sprunghafte HWND-Größen; ihre sichtbare Notch wird animiert.

`IWidget` definiert Identität, Modell, Initialisierung, Aktivität und Dispose. Rendering übernehmen typisierte WPF-DataTemplates in `WidgetTemplates.xaml`. Das vermeidet UI-Abhängigkeiten in den Services. `WidgetRegistry` aktiviert teure Dienste erst, wenn mindestens ein Monitor das jeweilige Widget tatsächlich im Dashboard zeigt.

Neue Widgets:

1. Einen beobachtbaren Service oder ein ViewModel hinzufügen.
2. `Widget<T>` ableiten; bei Bedarf `SetActive` für Hintergrundarbeit überschreiben.
3. Im `AppHost` bei `WidgetRegistry` registrieren.
4. Eine `DataTemplate` und den Settings-Eintrag ergänzen.

Ereignisse besitzen eine Priorität, Laufzeit und einen Schlüssel. Höhere Priorität unterbricht niedrigere, gleiche Schlüssel werden zusammengefasst, die Warteschlange ist auf 20 Einträge begrenzt. Nach Ablauf wird das nächste Ereignis angezeigt, danach der normale Fensterzustand. Erinnerungen und Timer werden unabhängig von Widget-Sichtbarkeit weiterverarbeitet.

Fenster und ObservableCollections werden auf dem WPF-Dispatcher aktualisiert. Windows-Ereignisse werden über `Ui.Run` weitergereicht. Medienabfragen sind durch ein Semaphore serialisiert. CPU-/RAM-/Netzwerk-/PDH-Abfragen laufen bei sichtbarem Widget einmal pro Sekunde im Hintergrund. Bluetooth nutzt DeviceWatcher; nur Akkuwerte ausgewählter verbundener Geräte werden zusätzlich einmal pro Minute gelesen. Die Discord-Integration wird ausschließlich über ihren Integrationsschalter aktiviert und verwendet Benachrichtigungsereignisse plus einen Abgleich alle fünf Sekunden.

Discord verwendet ab 0.2 `IDiscordNotificationSource` mit dem produktiven `WindowsDiscordNotificationSource`. Zugriffszustände werden getrennt von der Prozesserkennung modelliert. Paketidentität und `userNotificationListener` sind im MSIX-Manifest deklariert. Der Nutzer aktiviert einen sofort gespeicherten Schalter; nur dieser explizite UI-Vorgang fordert gegebenenfalls Windows-Zustimmung an. Ein Generation-Zähler verwirft verspätete Ergebnisse nach Ausschalten/erneutem Einschalten. Die Integration lauscht auch im Compact Mode und gleicht alle fünf Sekunden mit dem Mitteilungscenter ab. Nachrichten bleiben im Arbeitsspeicher. Siehe [Discord](DISCORD.md).

Geplante Module können denselben Widget-Vertrag verwenden: Kalender, Wetter, Gmail, GitHub, Twitch, OBS, Kameraaktivität, Downloads, Clipboard, Mixer und App Launcher. Dafür werden keine künstlichen Datenquellen oder leeren UI-Karten angezeigt.
