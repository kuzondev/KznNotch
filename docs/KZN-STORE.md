# Kzn Store

Der Store startet mit Kzn Notch und unterstützt beliebig erweiterbare Programmkataloge (bis zu 200 Produkte). Im Startmenü **Kzn Store** öffnen, über das Notch-Traymenü oder Einstellungen → Allgemein. Portable Ausgabe: `KznStore.exe` oder `KznNotch.exe --store`.

## Aktueller Umfang

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
