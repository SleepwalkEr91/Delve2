# Delve Plugin – ExileCore

An ExileCore plugin for Path of Exile that enhances Delve exploration by showing chest icons and directional radar lines on the minimap.

---

## Quick Install (PowerShell)

```powershell
irm https://raw.githubusercontent.com/Matejko-poe/Delve2/main/install.ps1 | iex
```

The script will ask for your ExileCore `Plugins/` folder path, then download and install everything automatically.

---

## Features

### Minimap Icons
Displays icons on the minimap for every Delve chest type, including:
- **Currency** — orbs etc
- **Fossils** — tiered by value (T1 / T2 / T3) with distinct icons
- **Resonators** — tiered (T1 / T2 / T3)
- **Azurite** — tiered (T1 / T2 / T3)
- **Dynamite & Flares** — mining supplies
- **Pathway chests** — generic loot along the path
- **Strongboxes, Maps, Gems, Unique items** and many more

### Radar Lines
When a chest is outside the visible minimap area, a directional line is drawn from the center of the minimap pointing toward it.

### Per-Type Filtering
Each chest category can be individually shown or hidden via the settings panel.

### Fossil Tier Icons
Fossils are sorted into three tiers based on value:
- **T1** — Hollow, Fractured, Faceted
- **T2** — Shuddering, Glyphic, Sanctified, Corroded, Dense
- **T3** — Jagged, Bound, Pristine, Serrated, Metallic, Aberrant, Aetheric, Prismatic, Fundamental, Scorched, Frigid, Lucent, Deft, Opulent, Bloodstained, Gilded, Tangled

---

## Manual Install

1. Download the latest release zip from [Releases](https://github.com/Matejko-poe/Delve2/releases/latest)
2. Extract the `Delve/` folder into your ExileCore `Plugins/` directory
3. Restart ExileCore

Required structure after install:
```
Plugins/
└── Delve/
    ├── Delve.dll
    ├── Fossil_Tiers.json
    └── Textures/
```

---

*Improvements made with the assistance of [Claude](https://claude.ai) (Anthropic).*
