# Gaming mode (0.4.3)

Settings > Spielmodus, or the tray's Spielmodus submenu:
- Aktiv: normal operation, with game detection polling stopped.
- Minimalistisch: during an active detected game, show only a 132 x 32 DIP clock in one of four monitor-relative corners. No expansion, priority event panel, or input capture. The window is click-through and does not activate.
- Aus während des Gamings (default): hide every configured notch window, including event UI. Tray settings remain accessible.

After leaving the game, restore the user's ordinary notch position, shape and selected function. Manual tray hiding is maintained independently. Game mode does not stop timers or reminders. Notification delivery is a separate Windows setting.

The detector samples the foreground process once per second. It uses the Windows exclusive Direct3D notification state, foreground game executable names, and Steam/Xbox/Epic installation path heuristics. It does not inspect game memory, inject modules, read input, or install hooks/drivers. Browsers and launchers are excluded. Own settings/tray focus preserves the prior game state; two negative samples restore normal operation to avoid flicker.

Windows does not expose a universal authoritative game detector. Unrecognized windowed or borderless games can be added by EXE name. Installation directory heuristics can also match non-game utilities stored alongside games; choose Aktiv if needed. Protected processes may restrict executable-path access. True exclusive fullscreen can prevent desktop overlays from being rendered, including the minimal clock.

Reference: https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-query_user_notification_state

Verification: injectable foreground detector tests simulate start, stop, own-window focus and quick focus transitions. Real WPF windows verify hidden visibility, event suppression, normal restoration, and all four corner positions. Existing media, Discord, AirPods and service regression checks also run. No actual game was launched for these automated tests.

## FPS ab 0.6

In Einstellungen → Spielmodus „FPS · Bildrate & Frametime“ wählen und sichern. Falls Windows den ETW-Zugriff verweigert, „FPS-Messung mit Windows-Freigabe starten“ anklicken und den UAC-Dialog selbst bestätigen. Danach ins Spiel zurückkehren. Die gewählte Ecke zeigt die reale, geglättete Bildrate. Unter Widgets lässt sich zusätzlich Gaming & FPS einschalten, mit Frametime und Status. Ohne Messdaten erscheint — FPS. Exklusives Vollbild kann das normale Desktop-Overlay verdecken; randloses Fenster ist dafür zuverlässiger. Keine Injektion, kein CSV-Mitschnitt. PresentMon wird mit Lizenz mitgeliefert. Für die Freigabe muss das erhöhte Konto zur gleichen Windows-Benutzeridentität wie die App gehören.

### Aktuell installierte Variante ohne Rechteerweiterung

Die Standardvariante 0.6.0 enthält keine allowElevation-Paketfähigkeit und fordert keine UAC-Rechte an. „FPS-Messung starten“ funktioniert nur mit bereits vorhandener Windows-ETW-Messberechtigung. Auf diesem Konto wurde dafür Zugriff verweigert; dann bleibt die Anzeige bei — FPS. Es wurden keine Gruppenmitgliedschaften oder Systemrechte verändert. Die separat vorbereitete Variante mit erhöhtem Helfer benötigt ausdrückliche Zustimmung vor der Installation.

## Automatische Spiele ab 0.7

Die manuelle Liste Eigene Spiele wurde durch automatisch gelesene Bibliotheken ersetzt: Steam, Epic, Xbox und GOG. Neue Installationen erscheinen spätestens bei der nächsten Aktualisierung (jede Minute oder per Button). Zusätzlich lokal gemerkt werden Spiele, die über ihren Bibliothekspfad oder Windows' exklusives Direct3D-Vollbildsignal erkannt wurden. Beliebige unbekannte Fensterprogramme lassen sich dadurch nicht zuverlässig als Spiele klassifizieren; Browser und Launcher bleiben ausgeschlossen. Keine manuelle EXE-Pflege mehr. Erkannte ausführbare Pfade werden lokal in detected-games.json gespeichert.
