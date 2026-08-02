# Delve Plugin – ExileCore

An ExileCore plugin for Path of Exile that enhances Delve exploration by showing chest icons and directional radar lines on the minimap.

---

## Features

### 🗺️ Minimap Icons
Displays icons on the minimap for every Delve chest type, including:
- **Currency** — orbs etc
- **Fossils** — tiered by value (T1 / T2 / T3) with distinct icons
- **Resonators** — tiered (T1 / T2 / T3)
- **Azurite** — tiered (T1 / T2 / T3)
- **Dynamite & Flares** — mining supplies
- **Pathway chests** — generic loot along the path
- **Strongboxes, Maps, Gems, Unique items** and many more

### 📡 Radar Lines
When a chest is outside the visible minimap area, a directional line is drawn from the center of the minimap pointing toward it — so you never miss a chest off-screen.

### 🎛️ Per-Type Filtering
Each chest category can be individually shown or hidden:

| Toggle | Description |
|---|---|
| **Delve Chests** | Master on/off for all chests |
| **Currency Chest** | Currency and coin chests |
| **Fossil Chest** | Fossil chests (all tiers) |
| **Azurite Chest** | Azurite vein chests |
| **Resonator Chest** | Resonator chests |
| **Dynamite Chest** | Mining dynamite supplies |
| **Flares Chest** | Mining flare supplies |
| **Pathway Chest** | Chests along the path (green line) |
| **Minimap Radar Lines** | Directional lines to off-screen chests |

> All toggles work independently — you can filter the minimap to show only what matters to you (e.g. fossils and resonators only).

### 🏆 Fossil Tier Icons
Fossils are automatically sorted into three tiers based on value, each with a distinct icon:
- **T1** — Hollow, Fractured, Faceted, Glyphic, Sanctified, Shuddering, Corroded
- **T2** — Dense, Deft, Aetheric, Pristine, Bloodstained, Bound, Jagged, Metallic, Fundamental
- **T3** — Gilded, Serrated, Aberrant, Frigid, Scorched, Prismatic, Tangled, Lucent, Opulent

---

## Installation

1. Download the latest release
2. Copy the `Delve` folder into your ExileCore `Plugins/` directory
3. Restart ExileCore

**Required files inside the folder:**
```
Delve/
├── Delve.dll
├── Fossil_Tiers.json
└── Textures/
```

---

*Improvements made with the assistance of [Claude](https://claude.ai) (Anthropic).*
