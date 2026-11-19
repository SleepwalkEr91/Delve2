# Delve Plugin - Changelog

## 2026-07-26 — Fix: ikony nie rysowały się po aktualizacji ExileCore

**Problem:** `GetAtlasTexture()` szukało tekstur w `Plugins\Temp\Delve\textures\`, ale nowy ExileCore nie kopiuje zasobów z Source do Temp.

**Zmiany w `DelveCore.cs`:**
- Dodano `ResolveSourceDirectory()` — szuka folderu Textures w `Plugins\Source\Delve\` (fallback na Temp)
- Dodano `_delveAtlas` (AtlasTexturesProcessor) ładowany z `CreateAtlas()` z jawną ścieżką
- Zamieniono `base.GetAtlasTexture(name)` → `GetDelveAtlasTexture(name)` (własna metoda)
- Fix: `"Strongbox.png"` → `"Strongbox"` (klucz w atlasie nie ma rozszerzenia)
- Null guard w `RenderMapImages()` — zabezpieczenie NullRef podczas loading screenów
- Wszystkie ścieżki do zasobów (Fossil_Tiers.json, chart icons) teraz z SourceDir
