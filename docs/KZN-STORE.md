# Kzn Store

Der Store startet mit Kzn Notch und unterstützt beliebig erweiterbare Programmkataloge (bis zu 200 Produkte). Im Startmenü **Kzn Store** öffnen, über das Notch-Traymenü oder Einstellungen → Allgemein. Portable Ausgabe: `KznStore.exe` oder `KznNotch.exe --store`.

## Aktueller Umfang

- App-Store-inspirierte Heute-Ansicht mit Vorschau, Apps-Liste, Mediathek und App-Detailseiten. Die Vorschau ist eine Illustration der Oberfläche.
- GitHub-Sterne aus dem jeweiligen Repository, klar als Community-Signal gekennzeichnet; keine erfundenen 5-Sterne-Bewertungen. Die Repository-API wird beim Öffnen und beim manuellen Updatecheck abgefragt, innerhalb eines geöffneten Stores maximal alle zehn Minuten automatisch. Fehler oder fehlende Daten erscheinen als „Nicht verfügbar“, eine echte Null bleibt eine Null.
- „Updates prüfen“ ist auf allen Store-Seiten sichtbar und prüft das gemeinsame Notch-/Store-Paket über GitHub Releases. Der Dialog übernimmt die gewählte Store-Sprache.

- Deutsch und Englisch, direkt umschaltbar und gespeichert. Die Systemsprache bestimmt die erste Auswahl; andere Sprachen verwenden Englisch.
- Entdecken, Suche, Mediathek, installierte Versionen, Updates und Öffnen installierter Apps.
- Kataloge ergänzen Programme anhand ihrer stabilen `Id`. Ein erneuter Import derselben Id aktualisiert den Eintrag.
- Downloads benötigen HTTPS und SHA-256. Paketname, Herausgeber und Version werden aus dem MSIX geprüft. Der Windows-App-Installer übernimmt Signaturprüfung, Installation und die Bestätigung durch den Nutzer. „Aktualisieren“ oder erneutes Aktivieren des Storefensters liest danach den Installationsstatus neu ein.
- Abbrechbare Downloads, maximal 1 GB pro Installer; unvollständige oder fehlerhafte Dateien werden gelöscht.
- Derzeit ausschließlich einzelne `.msix`-Pakete für Windows; EXE-Installer, Bundles, Konten, Zahlungen und ein gehosteter Onlinekatalog sind nicht enthalten.

Da noch kein öffentlicher Download konfiguriert ist, enthält der mitgelieferte Katalog keinen erfundenen Link. Die installierte Kzn Notch lässt sich öffnen. Für die Veröffentlichung einen echten HTTPS-Link und dessen Installer-Prüfsumme in den Katalog schreiben.

## Katalogformat, Version 1

```json
{
  "SchemaVersion": 1,
  "Products": [
    {
      "Id": "kzn-notch",
      "Name": "Kzn Notch",
      "Version": "0.8.0.0",
      "PackageName": "Notchify.Desktop",
      "Publisher": "CN=Notchify Development",
      "GitHubRepository": "kuzondev/KznNotch",
      "DownloadUrl": "",
      "Sha256": "",
      "Description": {
        "de": "Deine dynamische Windows-Notch.",
        "en": "Your dynamic Windows notch."
      }
    }
  ]
}
```

Version muss exakt der vierteiligen Version im Paket entsprechen. Leerer Download bedeutet „Noch kein Download“. Bei gesetztem Download sind ein HTTPS-Link und ein 64-stelliger SHA-256-Hexwert erforderlich. Den Hash erhältst du mit `Get-FileHash -Algorithm SHA256 <Installer.msix>`.

Weitere Programme als zusätzliche Objekte in `Products` anlegen. Ihre tatsächliche MSIX-Identität und den Signatur-Herausgeber verwenden. Beschreibungen unterstützen zusätzliche Sprachcodes; nicht vorhandene Texte fallen auf Englisch zurück. Weitere Oberflächensprachen werden zentral in `src/KznNotch/Store/StoreStrings.cs` ergänzt.

Der Store liest keine ausführbaren Befehle aus Katalogen und installiert nichts beim Import. Importierte Kataloge sind dennoch eine Vertrauensentscheidung: Sie bestimmen Herausgeber und Downloadquellen. Vor der Übernahme zeigt der Store die enthaltenen Programme an.

## Autostart

Die installierte App registriert `KznNotchStartup` als Windows-Startaufgabe. Beim ersten Start nach dem Update wird die vorhandene Einstellung „Mit Windows starten“ übernommen. Der Pfad überlebt Paketupdates. Die portable Variante verwendet weiterhin den Run-Eintrag des aktuellen Benutzers.

Wenn Windows die Startaufgabe durch den Benutzer oder eine Richtlinie gesperrt hat, zeigt die App den tatsächlichen Status. Einstellungen → Allgemein → Windows-Autostart öffnen führt direkt zur Windows-Verwaltung. Windows-seitige Deaktivierungen werden respektiert.

API-Grundlage: [Microsoft StartupTask](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.startuptask).

## Lokales Testitem

`KznNotch.exe --store-demo` aktiviert „KZN Test“ ausschließlich im aktuellen lokalen Store-Profil. Neue Profile zeigen es nicht. Es wird nicht in den Programmkatalog exportiert. In der Detailseite lässt sich das Item mit „Testitem ausblenden“ wieder deaktivieren.

Die Demo erzeugt pro geöffnetem Fenster zufällige, ausdrücklich gekennzeichnete Demo-Sterne. Installieren zeigt einen abbrechbaren Fortschritt und einen simulierten Abschluss; Testen zeigt eine Rückmeldung, Zurücksetzen ermöglicht einen weiteren Durchlauf. Es werden keine Installer heruntergeladen oder Programme installiert. Der simulierte Installationszustand gilt nur bis zum Schließen des Fensters. Der GitHub-Button zeigt den tatsächlichen Quellcode der Demo im Kzn-Repository.
