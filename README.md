# Kzn Notch

Eine native, dunkle Notch für Windows 11. C# · .NET 10 · WPF · MVVM.

**Neu in 0.9:** Automatische Updatesuche beim Start über [GitHub Releases](https://github.com/kuzondev/KznNotch/releases), Update-Dialog, abbrechbarer Download und Prüfung von SHA-256, Paketversion und Herausgeber. [Updates veröffentlichen](docs/UPDATES.md).

**Neu in 0.8:** Kzn Store als eigenes Startmenü-Programm, Deutsch/Englisch, erweiterbare Mehr-App-Kataloge, Suche, Mediathek und geprüfte MSIX-Downloads. Der MSIX-Autostart verwendet jetzt eine updatefeste Windows-Startaufgabe. [Store und neue Programme](docs/KZN-STORE.md).

**Neu in 0.7:** Kzn Studio mit eigener `.kzn`-Anzeigesprache, lokaler Plugin-Bibliothek und automatischer Spieleerkennung. [Eigene Plugins programmieren](docs/KZN-LANGUAGE.md).

**Discord:** Discord Integration ohne Entwicklerdaten. Unter **Einstellungen → Integrations** einschalten. Discord-Benachrichtigungen erscheinen mit Anzahl, Absender, Nachricht und Zeit. Dafür ist die installierte MSIX-Version mit einmaliger Windows-Freigabe erforderlich. [Installation und technische Grenzen](docs/DISCORD.md).

## Starten

Für Discord-Benachrichtigungen das MSIX aus `artifacts/KznNotch-installer` gemäß [Installationshinweisen](docs/DISCORD.md) installieren und **Kzn Notch aus dem Startmenü** öffnen. Für die übrigen Funktionen kann auch `Start-KznNotch.cmd` oder die portable EXE verwendet werden. Der portable Veröffentlichungsbuild enthält die .NET-Laufzeit; den gesamten entpackten Ordner zusammenhalten.

- **Hover:** Medienvorschau mit Wiedergabesteuerung.
- **Klick auf die Notch:** Dashboard mit Widgets öffnen.
- **Pfeil oben rechts / Escape:** einklappen.
- **Zahnrad / Rechtsklick auf das Tray-Symbol:** Einstellungen.
- **Doppelklick auf das Tray-Symbol:** Dashboard öffnen.
- **Beenden:** über das Tray-Menü. Das Schließen der Einstellungen beendet die Hintergrundanwendung nicht.

Startargumente: `--settings` öffnet die Einstellungen, `--expanded` das Dashboard. Eine Instanz pro Windows-Sitzung.

## Implementiert in 0.1

- Transparentes, rahmenloses Overlay, Always-on-top, ohne Taskbar-Eintrag. Hintergrundereignisse aktivieren das Fenster nicht; Texteingaben werden erst im manuell geöffneten Dashboard möglich.
- Compact / Hover / Expanded mit zentralen Cubic-Ease-Out-, Opacity-, Translation- und Blur-Übergängen. Windows' reduzierte Animationseinstellung wird berücksichtigt.
- Uhr mit optionalen Sekunden und Datum; Computerakku über `GetSystemPowerStatus`, ohne Akkuanzeige bei fehlender Batterie.
- Globale Windows-Mediensitzungen: Titel, Künstler, Thumbnail, Position, Dauer, Play/Pause, Vor/Zurück und Seek. Steuerelemente richten sich nach den Fähigkeiten des Players.
- Timer mit 5/10/25 Minuten, eigener Dauer, Pause, Reset und Pomodoro (25/5). Die nächste Pomodoro-Phase startet bewusst per Klick. Laufender Timer und Pause überleben Neustarts.
- CPU, RAM, GPU und Netzwerk. Systemwerte werden nur bei sichtbarem Systemwidget im Dashboard im Abstand von einer Sekunde abgefragt.
- Aufgaben mit Priorität und Fälligkeitsdatum, lokale Erinnerungen, Windows-Toasts mit Tray-Fallback und priorisierte Notch-Ereignisse.
- Bluetooth: gekoppelte Geräte auswählen, Geräteakku für Compact Mode festlegen, Verbindungsereignisse, Windows-Geräteeigenschaften und BLE Battery Service.
- Lautstärke- und Mikrofon-Mute-Ereignisse über Core Audio, inklusive Wechsel des Standardgeräts.
- Spotify über die normale Windows-Mediensitzung, ohne Anmeldung.
- Discord ohne Zugangsdaten: automatische Prozesserkennung, Windows-Mitteilungscenter, Benachrichtigungszähler und Nachrichtenvorschau. Standardmäßig deaktiviert; MSIX-Installation erforderlich.
- Einstellungen für Maße, Rundung, Deckkraft, Farben, Animationen, Offset, Monitor und Autostart. Compact- und Dashboard-Widgets per Drag & Drop oder Pfeiltasten-Schaltflächen sortieren und aktivieren.
- Hauptmonitor, bestimmter Monitor oder alle Monitore. Windows-DPI-Skalierung und Änderungen der Monitoranordnung werden berücksichtigt.

## Bauen und prüfen

Voraussetzung: Windows 11 x64 und .NET-10-SDK. Das lokal für diese Erstellung eingerichtete SDK liegt unter `.tools/dotnet` und wird vom Skript automatisch genutzt. Für einen frischen Checkout das offizielle .NET-10-SDK installieren; Windows-API-Referenzen kommen über NuGet.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1
powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Verify
powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Publish
```

`-Verify` führt einen Smoke-Test mit einem eigenen Datenordner aus: JSON-Wiederherstellung, Ereignisprioritäten, Aufgabenpersistenz, überfällige Erinnerungen, Timer/Pomodoro und echte Windows-Batterie-, Medien- und Systemabfragen. Ergebnisse stehen in `artifacts/verification/<id>/report.json`. Die App vor dem Ersetzen eines laufenden Builds im Tray beenden.

## Daten

```text
%AppData%/Kzn Notch/
  settings.json
  todos.json
  reminders.json
  timer.json
  kzn-notch.log
```

Atomisches Ersetzen der JSON-Dateien; beschädigte Dateien werden mit `.corrupt-<Zeitpunkt>` gesichert. Einträge im Log werden bei 2 MB rotiert. Keine Telemetrie. Autostart verwendet ausschließlich `HKCU/Software/Microsoft/Windows/CurrentVersion/Run/KznNotch`.

## Grenzen und Berechtigungen

- **Medien:** Ein Player muss seine Sitzung bei Windows registrieren. Reiner Audio-Output ohne SMTC-Sitzung liefert weder Songtitel noch Fernsteuerung. Browser, Spotify und VLC sind deshalb von ihrer jeweiligen Konfiguration abhängig. Es läuft kein Audio-Mitschnitt.
- **Bluetooth:** Geräte und Treiber müssen den Akkustand veröffentlichen. Proprietäre Protokolle etwa bestimmter Kopfhörer, Controller oder USB-Empfänger sind nicht universell lesbar. Nicht verfügbare Werte werden ausdrücklich so angezeigt. BLE-Abfragen sind auf ausgewählte, verbundene Geräte begrenzt, mit 5 Sekunden Timeout und 60 Sekunden Aktualisierungsintervall.
- **Discord:** Keine Application ID oder Tokens erforderlich. Benachrichtigungen werden über Windows erfasst. Voice-Channel, Discord-Mute/Deafen und Aktivität stehen unter diesen Bedingungen nicht zuverlässig zur Verfügung. Die alte manuelle OAuth/RPC-Anbindung wurde entfernt. Details in [DISCORD.md](docs/DISCORD.md).
- **Spotify:** Lautstärke und weitere Albumfunktionen über die Web API sind für später vorbereitet. Die Mediensteuerung von 0.1 braucht keine Spotify-App-Registrierung.
- **Transparenz:** WPF-Layered-Windows bieten transparente Pixel, jedoch kein zuverlässig kombinierbares DWM-Acrylic hinter dem gesamten Fenster. Die Blur-Option steuert deshalb den kurzen Inhaltsübergang. Es wird kein falscher Acrylic-Effekt versprochen.
- **GPU/Temperaturen:** GPU-Werte kommen aus Windows-PDH-Zählern und können bei fehlendem WDDM-Zähler ausfallen. Temperaturen werden nicht erfasst. Ab 0.6 gibt es prozessbezogene FPS und Frametime über PresentMon, bei Bedarf mit Windows-Freigabe; siehe docs/GAMING-MODE.md.
- **Benachrichtigungen:** Fokusassistent und Windows-Einstellungen können Toasts unterdrücken. Erinnerungen werden verarbeitet, während KznNotch läuft, und nach dem nächsten Start nachgeholt. Die App weckt keinen ausgeschalteten Rechner.
- **Klickdurchlässigkeit:** Flächen außerhalb der Notch blockieren keine Eingaben. Optionaler vollständig klickdurchlässiger Compact Mode wird über das Tray-Menü wieder interaktiv.
- **Performance:** Unter 1 % Idle-CPU ist ein Ziel und hardwareabhängig. Es gibt keine schnellen dauerhaften Polling-Loops; Render- und Systemstatistik-Timer pausieren bei eingeklappter Notch.

## Weiterentwicklung

Siehe [Architektur](docs/ARCHITECTURE.md), [manuelle Prüfungen](docs/VERIFICATION.md) und [Beiträge](CONTRIBUTING.md). Lizenz: MIT.

Primärquellen der Windows-Anbindungen: [Media Sessions](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager), [Bluetooth LE](https://learn.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice), [Core Audio Notifications](https://learn.microsoft.com/en-us/windows/win32/api/endpointvolume/nn-endpointvolume-iaudioendpointvolumecallback). Discord-Berechtigungen und Nachrichtenformat: [offizielle Discord-RPC-Dokumentation](https://github.com/discord/discord-api-docs/blob/main/developers/topics/rpc.mdx).
