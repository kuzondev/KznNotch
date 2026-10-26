# GitHub-Updates

Die App prüft bei jedem normalen Notch-Start im Hintergrund `https://api.github.com/repos/kuzondev/KznNotch/releases/latest`. Keine Anmeldung, keine eingebetteten Tokens. Ein kurzer Timeout und stille Behandlung von Offline-/API-Fehlern verhindern, dass der Start blockiert. Unter Einstellungen → Allgemein kann die Suche manuell gestartet werden.

Ein neueres stabiles Release mit passendem x64-MSIX löst den Dialog „Möchtest du jetzt aktualisieren?“ / „Do you want to update?“ aus. Ablehnen lässt die aktuelle Version unverändert. Nach Zustimmung lädt die App den Installer mit Fortschrittsanzeige und Abbruch herunter und prüft GitHubs SHA-256-Digest sowie MSIX-Paketname, Version und den bisherigen Herausgeber. Der Windows-App-Installer bestätigt und installiert das signierte Paket. Die bestehende Signaturvertrauenskette muss erhalten bleiben; ein neues zufälliges Entwicklungszertifikat pro Release funktioniert nicht als verlässliche Updatequelle.

## Releases veröffentlichen

Vom lokalen PC mit dem bestehenden Entwicklungsschlüssel: Änderungen committen, dann `powershell -ExecutionPolicy Bypass -File scripts/Publish-Release.ps1 -DevelopmentCertificate`. Der Befehl baut, signiert mit dem vorhandenen Schlüssel, pusht den Commit und erstellt das Release. Mit einem anderen bereits eingerichteten Signaturschlüssel stattdessen `-SigningCertificate` und `-CertificatePassword` übergeben.

1. Projektversion, Manifest und Dateinamen in den Build-/Paketierungsskripten gemeinsam erhöhen. Vierstellige Manifestversion entspricht beispielsweise `0.9.0.0`, Release/Projektversion `0.9.0`.
2. Änderungen committen und einen Tag wie `v0.9.0` pushen.
3. Die GitHub-Action baut und signiert das MSIX. Dafür einmalig die Repository-Secrets `KZN_SIGNING_PFX_BASE64` und `KZN_SIGNING_PASSWORD` mit deinem **vorhandenen** Signaturzertifikat konfigurieren. Private Schlüssel gehören niemals in Git oder Release-Assets.
4. Das veröffentlichte Release enthält `KznNotch-0.9.0-x64.msix`. GitHub erzeugt dessen SHA-256-Digest automatisch. Drafts und Prereleases werden nicht als normale Updates angeboten.

Ohne konfigurierte Signatur-Secrets baut die GitHub-Action nur und veröffentlicht kein Paket; der lokale Veröffentlichungsbefehl bleibt verfügbar.

Das erste Release wird lokal mit dem bestehenden Entwicklungsschlüssel erstellt. Dessen öffentlicher `.cer`-Teil kann für die ausdrücklich bestätigte Erstinstallation bereitgestellt werden. Für eine breite öffentliche Veröffentlichung ein geeignetes vertrauenswürdiges Signaturverfahren einrichten. Das Updater-Protokoll enthält keine Zertifikatspasswörter.

Portable Ausgaben erkennen ebenfalls Releases, verwenden zur Aktualisierung aber den MSIX-Installer; dadurch entsteht eine installierte Ausgabe. Ein Überschreiben laufender portabler Dateien ist nicht implementiert.

Referenz: [GitHub Releases REST API](https://docs.github.com/en/rest/releases/releases).
