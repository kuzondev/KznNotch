# Kzn 1 — eigene .kzn-Plugins

Kzn 1 ist die erste eigene Kzn-Anzeigesprache für Kzn Notch. Die Dateien sind normaler UTF-8-Text mit der Endung `.kzn`. Im Einstellungsbereich **Kzn Studio** lassen sich Plugins erstellen, prüfen, in der Vorschau betrachten, speichern, aktivieren und teilen. Ein aktives Plugin ersetzt die gesamte geschlossene Standardanzeige; Hover/Öffnen und die vorhandenen Funktionstabs bleiben verfügbar. Bei Plugins mit Buttons öffnet man die Funktionsansicht per Klick, damit die Buttons beim Darüberfahren erreichbar bleiben.

## Schnellstart: Bild statt Uhr

In Kzn Studio **Bild statt Uhr** wählen, ein PNG/JPG auswählen und **Speichern & aktivieren** anklicken. Das Bild wird in den lokalen Plugin-Ordner kopiert und erscheint fortan anstelle der Uhr. Die Vorlage **Bild statt Uhr** verwendet zunächst das vorhandene Kzn-Appsymbol.

```kzn
kzn 1
plugin "Mein Bild"
description "Mein eigenes Logo in der Notch"
target compact
image "assets/logo.png" 28
```

Bilder müssen im Plugin-Ordner liegen. Im Editor übernimmt **Bild einfügen** das Kopieren und ergänzt den richtigen Code. Unterstützt werden PNG, JPG/JPEG, BMP, GIF und ICO, bis 10 MB. GIF wird als einzelnes Bild dargestellt. Die Zahl ist die Größe in WPF-Pixeln; 28 passt zur normalen geschlossenen Notch. Für größere Bilder kann man die geschlossene Höhe unter Darstellung erhöhen.

## Eigene Schrift und Farbe

```kzn
kzn 1
plugin "Meine Uhr"
target compact
font "Consolas" 16
color "#A8BDFF"
clock "HH:mm:ss"
```

`font` verwendet den Namen einer installierten Windows-Schriftart. Eine globale Notch-Schrift lässt sich zusätzlich unter **Design → Notch-Schriftart** wählen; explizite Plugin-Schriften haben Vorrang für ihre eigenen Elemente.

## Dynamische Inhalte und Aktionen

```kzn
kzn 1
plugin "Mein Schnellzugriff"
layout row
spacing 8
font "Segoe UI" 12
clock "HH:mm"
button "Fokus" timer
when media.playing
text "{media.title}"
end
```

Bedingungen: `media.playing`, `timer.running`, `gaming.active`. In einem `when`/`end`-Block stehen Anzeigeelemente; verschachtelte Bedingungen werden in Kzn 1 nicht unterstützt.

Textvariablen: `{clock}`, `{date}`, `{media.title}`, `{media.artist}`, `{timer}`, `{fps}`. Die Werte werden aus den vorhandenen lokalen Diensten aktualisiert. `{fps}` zeigt ohne freigegebene Messdaten entsprechend — FPS.

Button-Aktionen: `media`, `timer`, `system`, `todo`, `reminder`, `bluetooth`, `discord`, `fps`, `settings`. Funktionstabs müssen unter Widgets eingeschaltet sein. Im Einstellungs-Vorschaubereich werden Button-Aktionen nur als Vorschau gemeldet.

## Befehle

| Befehl | Beispiel | Bedeutung |
|---|---|---|
| Version | `kzn 1` | Erste nichtleere Zeile |
| Name | `plugin "Name"` | Anzeigename |
| Beschreibung | `description "Text"` | Text in der lokalen Bibliothek |
| Ziel | `target compact` | Geschlossene Notch; Kzn 1 unterstützt dieses Ziel |
| Schrift | `font "Segoe UI" 14` | Installierte Schrift, Größe 8–32 |
| Textfarbe | `color "#F2F3F7"` | Farbe für Texte und Beschriftungen |
| Anordnung | `layout row` / `layout column` | Horizontal / vertikal |
| Abstand | `spacing 8` | 0–24 Pixel |
| Text | `text "Hallo {clock}"` | Statisch oder mit Variablen |
| Uhr | `clock "HH:mm"` | Lokales .NET-Uhrzeitformat |
| Bild | `image "assets/logo.png" 28` | Relativer Bildpfad, Größe 12–64 |
| Button | `button "Fokus" timer` | Vorhandene Notch-Funktion öffnen |
| Bedingung | `when timer.running` … `end` | Elemente bedingt anzeigen |
| Kommentar | `// Kommentar` | Ganze Kommentarzeile |

Text steht in doppelten Anführungszeichen; Escape-Zeichen folgen JSON (`\"`, `\\`). Relative Pfade am besten mit `/` schreiben. Höchstens 12 Elemente, 512 Zeilen und 64 KB UTF-8 pro Plugin.

## Lokale Bibliothek und Teilen

- Neue Vorlagen: Uhr, Medien und Fokus; außerdem der Bild-Assistent.
- **Prüfen & Vorschau** / Strg+Enter: kompiliert mit Zeilenfehlern und zeigt das Ergebnis.
- **Speichern** / Strg+S: speichert den Code. Bei einem bereits aktiven Plugin wird dessen gültige neue Version übernommen.
- **Speichern & aktivieren**: speichert und ersetzt die geschlossene Anzeige.
- **Standardanzeige**: deaktiviert das aktive Plugin dauerhaft und stellt Uhr/Standardwidgets wieder her.
- **.kzn importieren**: übernimmt Datei und ihre relativen Bilddateien in einen eigenen Bibliotheksordner.
- **.kzn exportieren**: erzeugt eine teilbare Datei. Bilddateien werden daneben in einen eindeutig benannten `assets`-Unterordner kopiert. Diesen Ordner mitgeben.

Plugins liegen lokal unter `%AppData%\Kzn Notch\Plugins`; bei MSIX wird dieser Pfad von Windows in den Paket-Datenbereich umgeleitet. Die Auswahl liegt in `plugin-state.json`. Die Bibliothek ist lokal und enthält die eigenen sowie importierten Plugins. Kzn 1 ist ein Anzeige-Interpreter mit festgelegten Befehlen; Dateien führen keine Shell-Kommandos, C#-Programme, Netzwerkanfragen oder DLLs aus.
