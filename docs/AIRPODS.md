# AirPods battery reception (0.4.1)

Independent C# implementation; no AirPodsDesktop source, assets, or dependencies were copied or translated. Its publicly documented protocol observations were used as research, then implemented using managed byte parsing and the official Windows BLE advertisement API.

Research sources:
- https://github.com/SpriteOvO/AirPodsDesktop
- https://github.com/SpriteOvO/AirPodsDesktop/blob/main/Source/Core/AppleCP.h
- https://learn.microsoft.com/windows/uwp/devices-sensors/ble-beacon

Enable Bluetooth and AirPods-Akkus in Settings > Geräte and save. Open the case nearby. Select the received BLE entry and save; optionally choose it for Compact Mode. Existing Windows driver and standard GATT battery paths remain unchanged.

The passive receiver reads Apple manufacturer proximity battery fields, including separate earbuds, case and charging flags. Values are approximate ten-percent buckets. Unsupported layouts, including AirPods Max, remain on Windows/GATT rather than being misinterpreted. Current supported identifiers cover AirPods 1/2/3/4 and Pro 1/2 (including USB-C).

Nearby reception does not establish ownership or a Windows connection. Rotating BLE addresses cannot safely be mapped to paired devices by display name, model, signal strength, or a battery pattern. Each broadcast address has a temporary session entry. Two earbuds may produce separate entries; they are deliberately not merged by heuristic. After address rotation or app restart the user must select the new entry. Session selections are cleared on settings load. No encrypted payload, raw radio address, or advertisement is logged or written to disk.

Only signals at least -70 dBm are admitted; this is a reception filter, not proof of ownership. Device updates are limited to every two seconds, a 15-second maintenance timer expires readings after 90 seconds, and discovery is bounded to 32 addresses. A silent case cannot be queried on demand using this format. An empty case field remains unavailable, not zero. Compact Mode uses the lower available earbud value; stale readings are removed. Bluetooth or AirPods disable/disposal detaches and stops the watcher.

Verification: synthetic packet fixtures cover both transmitting sides, separate charging flags, unavailable fields, invalid lengths/company, unsupported headband models, stale UI state and malformed inputs. Windows hardware reception depends on adapter support, radio state and actual nearby broadcasts; passing decoder tests does not confirm a particular physical AirPods pair.
