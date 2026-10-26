# Kzn Notch — desktop design contract

## Desktop Read

Windows-first background tray utility. Short mouse interactions with full keyboard access in settings and the manually opened island. Density: compact/standard. Evidence: existing WPF views, the running Windows application, and the requested Dynamic-Island/Fluent direction. No visual draft required. Scope: notch, hover, expanded widgets, settings and product rename; keep the service and data contracts.

## Design thesis

A quiet graphite utility that reveals just enough information, with Windows-native settings and a compact, continuous island transition.

- Keep the native title bar, system menu, resize, task switching and keyboard navigation on settings.
- Reserve custom window chrome for the notch. No web navbar, marketing headlines, hero copy or decorative metric tiles.
- Settings: 900 × 680 default, 760 × 560 minimum; 180 px left navigation, a scroll-owning content pane and a fixed action footer.
- Typography: Segoe UI Variable; 22 px page headings, 13 px settings labels, 12 px secondary text, 11 px widget headings. Timer numerals may use 30 px.
- Controls: 32 px normal height, 6 px corner radius, visible keyboard focus. Settings rows use 44–52 px height; labels and controls share a row when space permits.
- Surfaces: black/graphite island, one level of subtle inset grouping, 12 px widget corners. No nested card stacks. Accent is reserved for selection, progress and primary commands.
- Mica belongs to the standard settings HWND, with a readable fallback. The WPF layered overlay uses translucent tinted pixels; never label it as native Acrylic.
- Motion: short cubic ease-out shell changes, 4 px content translation; interruption begins from the current presented value. Reduced motion removes translation and blur. No looping decorative motion.
- Preserve Compact/Hover/Expanded, media controls, priority events, Bluetooth selection, Discord permissions, timers, task/reminder CRUD, widget sorting, tray, monitor selection and autostart.
- Long names truncate; message bodies wrap. Empty/permission/offline states stay adjacent to their controls. Counts describe their real Windows source.
- Keep the existing MSIX identity and publisher certificate for in-place updates and permission continuity; these are compatibility identifiers, not product branding. User-facing name, executable, projects and namespaces are Kzn Notch / KznNotch.
- Migrate existing settings, tasks, reminders and timer files without overwriting newer Kzn Notch data or deleting old files.

## Verification target

Build, service regression checks, rendered WPF states, installed responsive settings, keyboard focus and compact/hover/expanded behavior. Review long-text and denied-permission states. No fabricated telemetry or production messages for previews.

## Version 0.4 — single-function island

Apple-inspired restraint within a native Windows utility. Closed is the normal state; there is no Resting view. ScreenEdge attaches at the monitor top with square top corners; DynamicIsland floats using the configured vertical offset and rounds all corners. Hover opens only the last selected enabled function. Click pins that same view, without a dashboard expansion. The icon strip and previous/next buttons switch functions; Alt+Left/Right and Escape provide keyboard paths. Selection persists without rebuilding overlay windows. Open width uses HoverWidth; each function has a bounded height and owns overflow scrolling. System statistics run only while that function is visible on at least one monitor. Timers and reminders continue independently.

Neutral graphite surfaces, 10 px control corners, 16 px settings groups, 24 px page titles. No nested widget cards. Preserve Windows notification consent, media controls, settings compatibility and all service behavior. Reference: https://rama2555.github.io/nusa/#preview (Closed and Music interaction, without Resting).

## Version 0.4.2 — interaction polish

The notch uses dedicated IslandButton/IconButton/PrimaryButton styles, a compact inset function strip, neutral media progress and white primary playback control. Pointer enter scales controls to 1.035; press to 0.94 with 130 ms cubic recovery. These transforms never change layout or hit targets. Keyboard focus uses a separate accent outline. System reduced motion suppresses transforms; high contrast retains the focus and shell boundaries. Function changes translate ten pixels in navigation direction and fade in through the central animation service. No ambient or infinite animation. The timer uses light 40 px numerals and avoids duplicate section headings. Standard music view is 260 px high; the timer remains 340 px.

## Gaming presentation

Game-aware presentation is separate from manual tray visibility. The minimal gaming surface is a click-through clock, with no interactive controls or event takeover. It uses one of four monitor-relative corners and never steals input. Hidden gaming mode must also suppress priority-event surfaces and tray Expand requests; settings remain accessible. Returning to normal keeps the last selected function and configured attached/floating presentation.

## Apple Design foundation pass

See docs/APPLE-DESIGN-REVIEW.md and .design-rules/SKILL.md. Shared neutral surfaces, 13 px controls, 12 px secondary copy and 11 px metadata. Muted #AEAEB8 replaces dim media labels; music artwork is 64 px. Settings use stable navigation borders, neutral grouped rows and a simpler masthead. Scrollbars retain a narrow thumb within a 12 px pointer region. Windows conventions and existing service behavior remain authoritative. Light appearance and system text enlargement remain follow-up requirements.

## 0.4.4 — macOS-inspired settings and actionable island

This direction supersedes the earlier dark settings contract at the user's explicit request. The island stays dark; the entire settings window uses a light utility palette, an independent sidebar, category icon tiles, blue selected navigation, a pane title bar, white grouped settings and a fixed save area. Standard Windows title-bar controls remain functional and use light chrome.

Settings tokens: canvas #F5F5F7, sidebar #EAEAF0, group #FFFFFF, content #1D1D1F, secondary #626269, accent #0066D6. Segoe UI Variable Text 13 / secondary 12 / heading 21; 30 px controls and 38×22 switch rails within a 30 px hit region. 980×760 default; 820×600 minimum; 210 px sidebar. Both normal and minimum sizes are rendered in the smoke suite.

The function strip uses command buttons rather than focus-dependent ListBox selection. Each interactive press pins Hover to Expanded and cancels collapse before the control handles input. Selected state follows the view model for buttons, previous/next and shortcuts. Existing UI Automation regression checks invoke each real function button and assert its selected view. Physical mouse verification was attempted but Windows automation returned GetCursorPos access denied; do not claim this path has been verified.

Guidance loaded: .design-rules/references/hig/{accessibility,layout,typography,color,designing-for-macos,sidebars,buttons,settings,motion}.md and cross-platform.md. `layout.md › Visual hierarchy`: “Group related items to clearly express related information or functions.” `buttons.md › Content`: “Ensure that each button clearly communicates its purpose.” The specific palette and colored category tiles are design judgment. Light settings are an explicit visual direction in this pass; automatic dark appearance and full text scaling remain open.

## 0.4.5 — appearance and living island

Settings expose System / Hell / Dunkel with immediate preview and persistence on Save. System follows Windows AppsUseLightTheme and user preference notifications. Palette roles cover canvas, sidebar, groups, inputs, menus, separators, hover, focus, selection, sliders and switches; native title chrome follows the same appearance. High contrast substitutes system brushes. The user's explicit request for a Dark Mode selector takes precedence over the skill's default preference to omit an appearance switch.

Dark tokens: canvas #1C1C1E, sidebar #252528, surface #2C2C2F, input #38383C, text #F5F5F7, secondary #B5B5BF. The automated suite checks >=7:1 primary text contrast in both appearances and renders all seven panes in each.

Original WPF motion: critically styled, heavily damped response (approximately 1.2% overshoot), interruptible shell resizing from its presented dimensions; content fade + 3% scale + directional translation; track/cover reveal on title change; reveal for new priority events; short pane transitions. Repeated unrelated updates do not restart a settling shell. Animation 0 ms, system reduced animation and high contrast suppress shell motion. A stylized four-bar playback-state indicator runs at 25 Hz only while loaded, visible and playing with motion allowed. It measures no audio and performs no audio capture. Compact media may show its real cover. Timer accents use orange and larger right-aligned numerals.

Reference: https://github.com/jackson-storm/DynamicNotch#-gallery . Visually inspected only README Gallery screenshots Player, Timer, VolumeHud and Charging; their source files were not accessed or incorporated. Reference screenshots stay under artifacts/design-reference, outside distributable app assets. All shipped code and motion were written for this WPF project.

Guidelines reloaded: dark-mode.md › Dark Mode colors (semantic colors, contrast), motion.md › Providing feedback (brief, precise, cancellable transitions). Earlier always-load foundations and desktop references remain applicable. Static gallery images establish composition, not exact animation timing; the motion curve is our own design judgment.

## 0.4.6 — clock-only spacing

When the closed notch contains only the clock, measure its rendered content and remove the unused space reserved for other compact widgets. Clock-only height is capped at 32 px; the other compact layouts retain their configured dimensions. Hover and open function layouts remain unchanged.

## 0.4.7 — edge attachment correction

The user's spacing report concerned the monitor edge, not the content width. Revert 0.4.6 clock-only shrinking and restore configured compact dimensions. Minimal gaming clocks now attach at their selected monitor edge: top clocks have zero top inset and square top corners; bottom clocks align their bottom with the window bottom and have square bottom corners. Previously a fixed 8 px top margin left a gap despite the native window already being at monitor y=0.

## 0.4.8 — monitor-edge placement

Keep configured compact width and height. Reapply monitor-relative native placement after WPF's first layout, so startup placement cannot override SourceInitialized positioning. Remove the remaining fixed gaming top inset. Regression check measures the visible shell in screen coordinates after load against the physical monitor top.

## 0.4.9 — restore the original unoutlined shell

Remove the 0.75 DIP decorative shell border introduced in 0.4.5. It drew a light line along the top of an otherwise correctly positioned screen-edge notch. Normal appearance now uses the original borderless black shell; system high contrast retains its required visible boundary. Keep dimensions, appearance preferences and motion unchanged.

## 0.5.0 — consistent function panes

All manually opened and hover function panes use the same HoverWidth and a 340 px target height. Switching functions animates content only, with no shell-size change; long lists use the existing inner scroll region. Compact and temporary event surfaces keep their own roles.

Task composer precedes the list, with persistent labels for title, priority and optional due date. Reminder composer separates title, date and a readable 96 px time field; its create action uses the full width, empty errors collapse and empty state copy is explicit. Focus uses centered orange numerals, consistent preset buttons, an explicitly labeled custom-minute start action and a paired timer/Pomodoro action row. Existing service commands, persistence and validation remain intact.

Fix the ComboBox's nested ToggleButton to receive the parent's background/border instead of native light defaults. Dark date controls and input labels follow the shared island palette. No duplicated section headings inside these panes.

References loaded: entering-data.md › Best practices (“Be clear about the data you need.”), text-fields.md and layout.md › Visual hierarchy. At the user's request stable tab dimensions take precedence over earlier per-function sizing. Regression suite invokes each real function button, asserts identical dimensions and renders every function.

## 0.5.1 — music composition within the shared pane

Keep all tabs at the same 340 px height. Compose music to use its viewport: 108 px artwork, source and playback state above a two-line track title and artist, a 20 px seek hit region with elapsed/duration underneath, then larger centered transport controls. Remove the former cluster of small controls at the top and its unused lower half. Use real cover imagery as the visual signature; add no decorative filler. Long titles wrap to two lines and expose their full text in a tooltip.

## 0.6.0 — appearance workshop and game FPS

The Design pane exposes 14 independent color roles with native color dialogs, editable hex values and invalid-input feedback. A preview demonstrates text, progress, transport and selection colors. Changes affect the running island theme without persisting them; closing the settings discards the preview, Save commits a cloned settings snapshot. Original/Ocean/Forest presets, numerical foreground contrast, 2–8 px bar thickness, 4–24 px control corners and 340–460 px uniform open height complement existing size, position, opacity, motion and widget-order settings. Normal settings UI keeps its independent appearance palette for readability.

Color roles: background, accent/focus, primary text, secondary text/icons, progress fill, progress track, normal buttons/inputs, primary button background, primary button text, navigation background, selected tab background, selected tab icon, timer accent, borders/separators. The screen-edge shell remains borderless. All function panes continue sharing the same height, including previews. Optional Gaming/FPS function is enabled under Widgets.

FPS mode is enum value 3, preserving values of all existing gaming modes. It displays real target-process PresentMon frame intervals as a two-second smoothed FPS value in the click-through corner overlay. The optional Gaming pane adds frametime, executable name, source status and the permission action. It does not substitute monitor refresh rate. No samples means — FPS; samples expire after two seconds. Dominant swapchain selection avoids adding different swapchain rates together.

Official PresentMon Console 2.6.0 is bundled with its MIT license and valid Intel Authenticode signature. SHA256 B2A706BC6AD475749E3B7E3409263AA1E6906D45BDCF993F6DBC0F660188F1AF. The app's separate --fps-helper process can request UAC only from the explicit permission action. Packaged operation declares allowElevation for that helper. The main app remains asInvoker. The helper accepts integer process IDs via a random CurrentUserOnly duplex pipe, only launches the fixed bundled tool, stops capture outside an active game/FPS mode and terminates its own named ETW session when disconnected. There is no DLL injection, audio/video capture or frame CSV persisted to disk.

Validation: appearance serialization and normalization, unsaved preview/cancel behavior, presets and live common-height changes; CSV parsing, wrong PID/invalid interval rejection; actual helper pipe startup, explicit permission-error reporting and clean shutdown; complete prior smoke suite. All settings panes and the optional Gaming pane rendered. This Windows token denies ETW capture without elevation: elevated real-game FPS measurements are not verified in this session. Runtime UAC consent is left to the user. Exclusive fullscreen may cover any ordinary topmost desktop overlay.

References: local color.md, settings.md, entering-data.md, layout.md; customization density is the user's explicit preference. FPS backend docs: https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md ; Windows capability docs: https://learn.microsoft.com/windows/apps/package-and-deploy/app-capability-declarations .

### Installation without additional elevation capability

The automatic approval reviewer rejected installation of the allowElevation variant because that persistent security-boundary extension requires explicit human authorization. The signed review artifact is retained at artifacts/KznNotch-installer/KznNotch-0.6.0-with-elevation-review.msix. The default source/package omits allowElevation and contains no runas invocation. Customization and the standard-permission FPS helper are unaffected. On this account standard ETW capture returns access denied; elevated game measurements remain unavailable until separately approved. No user group or system permission was changed. This section supersedes the earlier UAC installation instructions for the default package.

## 0.7.0 — Kzn Studio and automatic games

Replace the manual executable list with a local game catalog. Read Steam app manifests and libraryfolders.vdf, Epic .item manifests, XboxGames folders and GOG registry installations at startup and once per minute. Known library paths and the existing Windows D3D fullscreen signal identify foreground games; observed executable paths persist locally. Do not classify arbitrary unrecognized windowed apps as games. Remove the GameExecutables model property and manual settings field. Existing settings files remain readable; their obsolete field is ignored. Browser/launcher exclusions remain.

Kzn Studio is an additional settings pane with a local plugin library, monospace top-aligned code editor, native image picker, templates, compiler diagnostics with line numbers, preview, save/activate/deactivate and import/export. Kzn 1 is a newly implemented UTF-8 text DSL supporting compact rendering, text, clock, local images, installed fonts, colors, spacing/layout, conditions, service variables and whitelisted action buttons. The separate compiler and renderer are documented in docs/KZN-LANGUAGE.md. One compact plugin is active at a time. This is a local library, not a network marketplace.

KznSurface subscribes to existing data services only while loaded and avoids rebuilding image assets on each clock tick. Image decoding is bounded and files are loaded without holding locks. Display code cannot run arbitrary host code, load font URLs or escape its asset directory. Export copies assets under a fresh relative subfolder. Invalid edits retain the last valid saved/active plugin. Buttons in compact plugins retain pointer access instead of disappearing during automatic hover expansion; they open the appropriate enabled function. Preview buttons report actions without changing the running window.

Global Notch font is selectable from installed Windows fonts with live preview; plugin-specific font directives retain priority. Restore standard compact content when the plugin is disabled or invalid on startup. Do not change uniform open-tab dimensions or the borderless shell. No package elevation capability or new elevated execution is added.

Validation: parser/condition/action/font support, unknown commands and external-path rejection, active-plugin restart persistence, invalid-save rollback, image import and portable export, disable persistence; actual WPF compact image replacement and per-plugin font rendering; real routed-pointer plugin action; manifest-based Steam/Epic/Xbox discovery and learned-game persistence; prior service, appearance, FPS permission-error and window regression suites. Visual checks cover settings, compact image and font states. HIG references used: entering-data, text-fields and layout; the language/editor follows the user's explicit extensibility request.

## Settings — iOS-inspired grouping

The settings panes follow the supplied dark reference while retaining Windows title-bar controls, resizing, mouse and keyboard access. Use a deep graphite grouped-list canvas (#101216 / #15181D), inset raised groups (#1D2026), subtle separators, a large page title, muted section labels and a clear blue selected sidebar row. Use consistent monochrome Fluent icons, crisp blue focus/selection and green enabled switches. Keep the persistent save action visible and adapt its inset when the sidebar compacts. Light and System remain available as optional appearances; Dark is the default for new settings files. Do not reproduce the reference's macOS window buttons.

## Kzn Store — 0.8

A separate desktop window and Start-menu entry provides a scalable catalog of Kzn applications. Keep discovery and the installed library in a compact sidebar, one searchable product list, descriptive publisher/version metadata and one clear contextual action per app. Use the system Light/Dark palette, 8px action corners, 14px product groups and visible keyboard focus. No invented future apps or download addresses. German and English switch immediately, persist independently and share stable translation keys; missing product translations fall back to English. Future apps are data in a validated versioned catalog, not new hardcoded screens. Installation remains an explicit product action using Windows App Installer after hash and identity validation. Import confirms the actual product/publisher list. Autostart reports the actual Windows startup-task state and respects Windows-side disabling.

Validation includes multi-app catalogs, translation fallback, package identity/publisher checks, HTTPS requirements, tampered-download cleanup, language switching/persistence, localized search empty states and real WPF renders at normal/minimum widths. HIG routes used: layout, accessibility, typography, color, macOS, sidebars, settings and dark mode, translated to WPF desktop controls.

## GitHub updates — 0.9

Check the fixed public GitHub release source asynchronously once per Notch start. Keep startup usable and show no offline-error prompts. A newer compatible stable release offers an explicit Yes/No update dialog with old/new versions and No as the default. Download progress supports cancellation; identity, publisher and GitHub SHA-256 validation precede the standard Windows App Installer. A manual check lives in General settings. The bilingual update wording follows the OS UI language. No embedded GitHub token or private signing key is distributed.

## Store — iOS App Store direction, 0.10

Use an editorial Today screen, a compact bottom tab bar (Today, Apps, Library), large 32px headings, a single illustrated Notch feature and real product rows. App names open detail pages; download/open actions remain separate pill buttons. Detail pages expose GitHub stars, package version and platform with a clearly labelled preview. Do not invent ratings, apps, reviews or account profiles. GitHub stargazers are a count, never a five-star rating. Failures show Unavailable; zero is shown only when returned by GitHub. The manual update action stays visible on every page and respects the selected language. Preserve native Windows title-bar, keyboard, resizing, system appearance, contrast and reduced-motion behavior. Accent text is #70B5FF in dark mode and #0062C4 in light mode. Keep the existing Kzn eye mark crisp with native vector geometry.

HIG references loaded: accessibility, layout, typography, color, iOS, macOS, tab-bars, buttons, dark-mode, motion and cross-platform translation. This adapts the iOS App Store's hierarchy and product presentation to WPF desktop. Verification covers a 640px window, DE/EN switching, detail/back navigation, the manual update command, explicit GitHub-source labels and zero versus unavailable data. Visual captures use a labelled test fixture of 42 stars; production fetches actual counts from kuzondev/KznNotch.
