# Kzn Store

Kzn Store is a separate Windows app and source repository: [github.com/kuzondev/KznStore](https://github.com/kuzondev/KznStore). It has its own project, settings, downloads, installer, release workflow, and live app catalog. It can run without Kzn Notch. When both are installed, Kzn Notch opens the Store from its tray menu or settings.

The Store reads `catalog/store.json` from its own repository at launch, when **Katalog aktualisieren** is selected, and every 15 minutes. It keeps the last valid catalog on the device for offline use. New products committed to that catalog appear in Store without rebuilding or reinstalling the Store app.

To publish a product, add it to `Products` in `catalog/store.json` with a unique ID, localized description, real package identity, HTTPS MSIX URL and its SHA-256. An empty download URL keeps an app listed until a signed installer is available. The Store verifies the checksum and package identity before asking Windows App Installer to present the installation confirmation.

The Store source is in `C:\Users\Mehme\Downloads\kzn_store`. See its README for build, package, and release instructions.
