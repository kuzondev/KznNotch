# Prüfungen

`scripts/build.ps1 -Verify` führt automatisierte Prüfungen mit separaten JSON-Dateien aus. Hardwareabhängige Fähigkeiten müssen ergänzend auf den Zielgeräten geprüft werden.

## Manuell

- Notch starten: kleines Fenster oben mittig, kein Taskbar-Eintrag; unterliegende Apps außerhalb der sichtbaren Notch bleiben bedienbar.
- Hover, Maus verlassen, Klick und Escape: Größenübergänge, zeitverzögertes Einklappen, manuell geöffnetes Dashboard bleibt offen. Eingaben in andere Apps werden durch Medien- oder Lautstärkeereignisse nicht unterbrochen.
- Medien mit einem unterstützten Player starten: Cover, Titel, Status, Pause, Vor/Zurück und Seek. Danach Player schließen. Player ohne SMTC darf keine erfundenen Metadaten erzeugen.
- Timer kurz konfigurieren, pausieren und KznNotch neu starten; Zustand wird geladen. Pomodoro wechselt nach Ablauf auf 5 Minuten Pause. Zeit im Schlafmodus wird nach dem Aufwachen berücksichtigt.
- Erinnerung einige Minuten voraus anlegen: Windows-Toast und priorisierter Hinweis. Nach Neustart keine zweite Benachrichtigung für dieselbe bereits ausgelöste Erinnerung.
- Aufgaben mit Datum und Priorität hinzufügen, markieren und löschen. Einstellungen speichern, Prozess neu starten und Persistenz kontrollieren.
- Bluetooth-Geräte in den Einstellungen auswählen. Bei unterstützten Geräten Akku prüfen; bei nicht unterstützten Geräten muss „Nicht verfügbar“ erscheinen. Verbindung trennen / wiederherstellen und Compact-Gerät wechseln.
- Discord Integration in der installierten MSIX-Version einschalten und Windows-Freigabe erteilen/ablehnen/widerrufen. Echte Discord-Toasts empfangen, in Windows entfernen, Anzahl und Vorschauen abgleichen. Beim Ausschalten müssen alle Vorschauen sofort verschwinden. In der portablen EXE muss der Installationshinweis erscheinen.
- Lautstärke ändern, Standard-Audiogerät wechseln und das Windows-Standardmikrofon muten. Dieser Zustand darf nicht als Discord-Mute ausgegeben werden; Discord-Voice-Daten werden ohne autorisierte API nicht angezeigt.
- Einstellungen: Widgets ziehen und mit den Pfeil-Schaltflächen verschieben, aktivieren/deaktivieren, Farben, Größen, Animation 0, Position, Offset und Klickdurchlässigkeit ausprobieren.
- Mehrere Monitore mit unterschiedlicher DPI-Skalierung, negativem Bildschirmursprung, Hotplug und Änderung des Hauptmonitors prüfen. Autostart nach Windows-Anmeldung kontrollieren.
- CPU-Auslastung mindestens 30 Sekunden nach Start im eingeklappten Idle beobachten. GPU-, Netzwerk- und Systemwerte sollen bei verborgenem Widget nicht weiter abgefragt werden.

## Bereits geprüft während der Erstellung

Release-Build und eigenständig startbarer x64-Build. Automatisierte Prüfungen für Persistenz, Recovery, Prioritäten, Reminder, Timer, Batterie-, Medien- und System-API-Zugriff. Ab 0.2 zusätzlich Discord-Filter, Vorschautexte, Sortierung, Deduplizierung, Entfernung, Freigabewiderruf, Ausschalten bei laufender Abfrage und fehlende Paketidentität. Native Einstellungen und Dashboard wurden gestartet. Das MSIX wird mit Microsoft MakeAppx validiert und lokal selbstsigniert. Der Empfang echter Discord-Toasts mit Windows-Zustimmung benötigt die Installation und Freigabe durch den Nutzer; dies wird nicht durch synthetische Testdaten als Ende-zu-Ende-Erfolg ausgegeben. Auf diesem Rechner existiert kein Computerakku. Geräteabhängige Features und gemischte Monitor-DPI sind keine pauschal bestätigten Kompatibilitätszusagen.

Eine 30-Sekunden-Messung des portablen Builds im Compact Mode auf diesem Rechner mit 12 logischen Prozessoren ergab 0,022 % CPU bezogen auf den gesamten Rechner (0,26 % eines Kerns), 159,7 MB Working Set und 88,3 MB privaten Speicher. Das ist eine Einzelmessung unter der vorhandenen Systemlast, keine Garantie für andere Rechner. Messdaten: `artifacts/verification/final/idle-performance.json` im Projektordner.
