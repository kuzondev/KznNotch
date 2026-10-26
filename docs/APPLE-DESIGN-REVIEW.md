# Apple Design redesign — Kzn Notch

Thesis: a quiet, single-purpose island for music, focus and quick tasks; settings read as a desktop instrument panel. WPF on Windows; Apple foundations apply, Windows window chrome, Segoe typography and Fluent symbols remain appropriate.

## Review and direction

- High: media attribution and elapsed time use #6F6F77 / #77777F at 10 px. Raise to #AEAEB8 and 11 px. `accessibility.md › Vision`: “Strive to meet color contrast minimum standards.”
- Medium: repeated blue-gray fills compete with the neutral island. Consolidate content, input and separator roles. `color.md › Best practices`: “Use color consistently throughout your interface.”
- Medium: settings selection changes borders on hover, moving content. Reserve borders and use a persistent selected surface. `motion.md › Providing feedback`: “Aim for brevity and precision in feedback animations.”
- Medium: the scrollbar is only 4 px wide. Give it a 12 px pointer region while keeping the visible thumb narrow. `accessibility.md › Mobility`: “Offer sufficiently sized controls.”

## Foundation

Dark surface #171719; raised #232326; controls #303034; content #F5F5F7; secondary #AEAEB8; accent #A8BDFF. Light counterparts for a future appearance pass: #F5F5F7, #FFFFFF, #E5E5EA, #1D1D1F, #595963, #2457C5. This pass retains the existing dark appearance; system light appearance remains outstanding.

Type: Segoe UI Variable Text 13 body / 12 secondary / 11 metadata; Display 24 page / 40 timer. Avoid thin small type. Signature: artwork-led music with a single white playback key; no decorative accent gradients. Motion: existing interruptible shell transition; no added motion.

Regular settings: [184 navigation | 28 inset / heading / grouped rows / 28 inset], fixed save footer.
Compact settings: [168 navigation | 20 inset / wrapping labels / controls], vertical scrolling within the pane.
Island: [function + time / settings + close] [artwork + title] [playback] [position] [function strip].

The structure belongs to a screen-edge utility: one function at a time, no overview dashboard or decorative statistics. Remove the redundant settings label in the product masthead; retain the page heading. Preserve integrations and all service contracts.

## Verification

Use existing WPF rendering and service smoke suite. Inspect music, timer and settings PNGs. A passing build does not establish screen-reader behavior, 200% text scaling or full system appearance support.

Measured dark raised-surface contrast (#232326): content #F5F5F7 14.39:1; secondary #AEAEB8 7.12:1; default accent #A8BDFF 8.50:1. User-configured accent remains supported. Render inspection showed settings grouping and text hierarchy working; music spacing was tightened to avoid needless overflow at its fixed height.

## Second pass: full settings redesign

Supersedes the initial foundation pass. See DESIGN.md, section 0.4.4. All seven panes now share a dedicated light settings theme and a custom two-pane shell. Pane content explicitly resets foreground inheritance so white selected navigation labels cannot leak into the settings forms. Existing product functionality, bindings and permissions remain intact. Button regression tests now invoke actual WPF Button automation peers instead of testing only view-model commands. All settings panes and minimum-size layout are rendered.

Validation 0.4.4: release build and publish succeeded; all service, UI Automation button, routed-pointer pinning, persistence, gaming and minimum-width checks passed. All seven settings pages were rendered and inspected. Installed MSIX version verified as 0.4.4.0. Native physical mouse automation remained unavailable due to GetCursorPos access denied.

0.4.5 validation: release build passed; full smoke suite passed, including appearance round-trip persistence, >=7:1 primary text contrast in light and dark, button commands and hover pinning, spring interruption and explicit 0 ms motion removal. Rendered all seven panes in both appearances and inspected dark appearance, integrations, media and timer; corrected inherited dark-heading colors and removed timer overflow. Physical input verification remains limited by the Windows automation access error documented above.
