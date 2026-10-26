# Discord Integration (0.2)

KznNotch fragt weder Application ID noch Tokens oder Entwicklerdaten ab.

1. Die **installierte MSIX-Version** von KznNotch aus dem Windows-Startmenü öffnen.
2. **Einstellungen → Integrations → Discord Integration** einschalten. Die Änderung wird sofort gespeichert, das Discord-Widget automatisch aktiviert.
3. Windows' einmalige Freigabe für den Benachrichtigungszugriff bestätigen. Diese Systemfreigabe kann eine Anwendung nicht sicher umgehen.

Danach erkennt KznNotch automatisch Discord, Discord PTB, Canary und Development anhand laufender Prozesse. Die Windows-Schnittstelle `UserNotificationListener` liest vorhandene Discord-Toasts aus dem Mitteilungscenter. Compact zeigt deren Anzahl; Hover die drei neuesten, das Dashboard bis zu 20 Meldungen mit Absender, Nachricht und Erstellungszeit. Die Zahl bezeichnet **Benachrichtigungen im Windows-Mitteilungscenter**, nicht Discords interne ungelesene Nachrichten. Gruppierte Meldungen werden als eine Windows-Benachrichtigung gezählt.

Windows liefert nur Inhalte, die Discord tatsächlich als Windows-Benachrichtigung veröffentlicht hat. Discords Desktop-Benachrichtigungen müssen aktiviert sein. Wenn Discord einen geöffneten Chat nicht benachrichtigt, Vorschauen verbirgt oder Windows keine Meldung aufbewahrt, kann KznNotch den Nachrichteninhalt nicht ergänzen. Eine Discord-Nachricht erzeugt nicht zwingend einen Windows-Toast.

Abgewiesene oder widerrufene Freigaben werden angezeigt. Das Ausschalten stoppt die Integration und leert alle im Arbeitsspeicher gehaltenen Meldungen, auch bei noch laufenden Abfragen. Nachrichten werden weder auf die Festplatte geschrieben noch an Server übertragen. Andere Apps werden anhand der Absenderidentität herausgefiltert, bevor Nachrichtentexte gelesen werden. Die Windows-Freigabe selbst ist technisch breiter und erlaubt dem Listener Zugriff auf Benachrichtigungen aller Apps; Windows bietet keinen Discord-spezifischen Berechtigungsschalter.

Benachrichtigungsereignisse werden direkt übernommen; alle fünf Sekunden erfolgt ein Abgleich gegen das Mitteilungscenter und den Prozessstatus. Das funktioniert auch bei eingeklappter Notch und geschlossenem Discord, sofern noch alte Meldungen im Mitteilungscenter vorhanden sind.

## Voice und Aktivität

Voice-Channel, Teilnehmer, Discords eigener Mute-/Deafen-Zustand und Discord-Aktivität sind mit den hier verlangten Bedingungen nicht zuverlässig verfügbar. Discords offizielle Voice-RPC-Schnittstelle benötigt eine autorisierte App; die alte manuelle Token-Anbindung wurde entfernt. KznNotch verwendet kein Token-Auslesen, Selfbot, Client-Injection, Datenbank-Auslesen oder UI-Scraping. Der Windows-Mikrofonstatus ist ein anderer Zustand und wird nicht als Discord-Mute ausgegeben.

## Installation und Distribution

`scripts/package-msix.ps1` erstellt ein MSIX mit `userNotificationListener` und `runFullTrust`. Ein frischer Checkout benötigt dafür Microsofts Windows-SDK-BuildTools. `scripts/build.ps1 -Publish` erstellt vorher den eigenständigen x64-Build. Für Produktion eine Store-/Produktionssignatur verwenden; mit `-SigningCertificate` kann ein eigenes zum Manifest-Publisher passendes Zertifikat übergeben werden.

Das hier erzeugte **lokale Entwicklungspaket** ist selbstsigniert. Windows installiert es erst, wenn dieses Entwicklungszertifikat ausdrücklich als vertrauenswürdig hinzugefügt wurde. Es wird beim Bauen niemals stillschweigend ein Zertifikat installiert. Für diesen lokalen Testbuild liegen MSIX, öffentliches `.cer` und `Install-KznNotch.ps1` zusammen in `artifacts/installer`. Ein Administrator kann dort explizit `./Install-KznNotch.ps1 -TrustDevelopmentCertificate` ausführen. Der private Schlüssel bleibt ausschließlich in `.tools/signing` und gehört niemals in ein Distributionspaket.

Die bisherige portable EXE bleibt nutzbar, kann aber mangels Paketidentität keinen Windows-Benachrichtigungszugriff bekommen und zeigt dafür einen konkreten Installationshinweis. Ein versprochenes „einfach einschalten“ gilt für die installierte Version nach Windows-Zustimmung, nicht für eine lose EXE.

## Quellen

- [Microsoft Notification Listener und erforderliche Zustimmung](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/notification-listener)
- [Manifest-Capability userNotificationListener](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap3-capability-manual)
- [MSIX-Signierung und Entwicklungspakete](https://learn.microsoft.com/en-us/windows/msix/package/sign-msix-package-guide)
- [Offizielle Discord-RPC-Berechtigungen](https://github.com/discord/discord-api-docs/blob/main/developers/topics/rpc.mdx)
