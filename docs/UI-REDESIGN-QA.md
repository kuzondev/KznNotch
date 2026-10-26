# Kzn Notch desktop redesign QA

## Desktop QA

- Result: passed for the implemented Windows desktop scope, with the manual coverage limits below.
- Source read: Windows-first background tray utility; compact density, mouse plus keyboard; preserve the existing functional contracts.
- Source evidence: original WPF views, running application, the user's requested native utility direction, and `DESIGN.md`.
- Implementation evidence: `artifacts/verification/kzn-layout` rendered compact/expanded/settings views; 28 passing existing regression checks; installed MSIX 0.3.1.0 with responsive `Kzn Notch · Einstellungen`; native capture of the installed settings window.
- Native feel: retained system title bar, menus, resizing, task switching and native WPF control semantics. DWM Mica requested only on the non-layered settings window; the layered notch uses a tinted translucent fallback.
- Layout: explicit settings navigation, scroll-owning content and fixed save footer. Viewport-aware widget layout fixes narrow/DPI/scrollbar wrapping. Timer/system pair remains compact; wide modules span the island.
- Typography/density: 22 px settings headings, 13 px rows, 12 px metadata, 32 px controls, reduced widget padding and 30 px timer numerals.
- State handling: disabled transport commands, empty media state, Discord permission/install/denied status, unavailable battery values, focus, selection and drag target feedback retained.
- Keyboard: installed settings Tab navigation and slider arrow keys reviewed; a Right-arrow step changed rounding from 24 to 25. Non-interactive row wrappers were removed from tab order. Test edits were not saved.
- Motion: compact/expanded rendering verified, current-value cubic transitions implemented, hover intent delay and reduced-motion fallback retained. Detailed frame timing across GPUs was not benchmarked.
- Brand: product name, title, tray, namespaces, executable, solution, projects and launcher renamed. The old MSIX identity/certificate remain only as compatibility identifiers, preserving the installed app's update and permission continuity.
- Data: old settings copied to both normal and package-redirected renamed folders. Originals retained and existing destination files never overwritten.
- Light mode: not part of this dark-first brief. Central high-contrast brushes exist; a full Narrator/high-contrast and mixed-DPI hardware matrix has not been certified.
- macOS native depth: not applicable.

## Limits

The regression checks cover real Windows battery/media/system API initialization and isolated task/timer/reminder/Discord state behavior. An active player, Bluetooth battery hardware, actual Discord notification consent and different multi-monitor DPI combinations were not re-established merely for the visual redesign. No change to those integrations' service contracts is claimed. Windows UI Automation text extraction was unavailable in the installed capture session, so keyboard behavior was inspected visually rather than claiming a full accessibility audit.

## Addressed findings

- Oversized marketing-like settings copy → task-oriented headings and labeled rows.
- Inconsistent rounded controls → shared 6 px control geometry and compact sizing.
- Card nesting in notification/device lists → divider-based rows within a single grouping level.
- WrapPanel rounding/scrollbar layout issue → viewport-measured widget placement.
- Focusable settings row wrappers → non-focusable wrappers; native controls remain in the Tab path.
- Fractional rounding steps with integer display → explicit one-unit rounding steps and 25 ms animation steps.
- MSIX data-shadowing during rename → installer-side migration, with preserved originals.

## 0.4 interaction revision
- Closed, attached notch and floating island verified with rendered native WPF states.
- One selected function on hover/click; bounded music and timer heights, persistent selection and previous/next navigation covered by smoke checks.
- Native service regression suite still checks timers, reminders, tasks, Windows media, CPU/RAM, and Discord consent/filter/count behavior.
- Existing package identity and certificate retained for updates. No fabricated media or device values used in visual verification.

## 0.4.2 notch polish
- Reviewed rendered music and focus views; removed duplicate timer heading and empty media status spacing so primary actions remain visible.
- Dedicated rounded notch controls have hover/press transforms and independent keyboard focus rings. Windows reduced-motion preference disables transforms.
- Regression suite passed including AirPods decoder, Discord and native Windows services. Added real-time checks for intermediate shell width and interruption/collapse completion.
- Evidence: artifacts/verification/notch-polish-042-release.
