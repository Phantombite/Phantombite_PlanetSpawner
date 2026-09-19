# DEV Funktion — Phantombite PlanetSpawner

Stand: 2026-09-19 · Version 1.0.0 · Workshop-ID 3723481681 · Core-Kanal 1995010

## Zweck
Spawnt Mod-Planeten (z. B. Sulvax, Pandora) beim Serverstart an festen Koordinaten, falls sie in der Welt fehlen.
Ersetzt das frühere `Core_PlanetSpawner` (aus dem Core ausgezogen).

## Ablauf
1. **Phase 0:** Alle geladenen Planeten-Definitionen auflisten. Neue Mod-Planeten (nicht Vanilla) werden mit
   `Enable=false` automatisch in die Config eingetragen.
2. **Phase 1:** Config prüfen. Ein Eintrag gilt als gültig, wenn er aktiv ist, `SubtypeId`, `Diameter` und `Seed`
   hat und die Definition existiert.
3. **Phase 2:** Für jeden gültigen Eintrag prüfen, ob ein Voxel-Objekt mit passendem Namen existiert
   (`<Subtype>-<Seed>d<Durchmesser>`, oder ein anderer Seed/Durchmesser desselben Typs `<Subtype>-...`).
   Fehlt es, wird es mit `VoxelMaps.SpawnPlanet` an `Position − Durchmesser/2` gespawnt.

## Konfiguration
`PlanetSpawner_Config.ini` im World-Storage (nur Server). Pro Planet: `Enable`, `SubtypeId`, `Diameter`, `Seed`,
`PosX/PosY/PosZ`. Einträge ohne Koordinaten werden als „Koordinaten fehlen!“ gemeldet.

## Commands (`!pbc planetspawner ...`, beide nur Admin)
| Command | Wirkung |
|---|---|
| `check` | Planeten prüfen und fehlende spawnen |
| `list` | Alle konfigurierten Planeten auflisten |
Die Befehle werden in eine Queue gestellt und im nächsten Simulations-Tick ausgeführt (nur dort funktionieren `VoxelMaps`).

## Core-Anbindung
Empfängt `READY`, `LOGLEVEL`, `PERFLEVEL` (kein Throttle, wird nur bestätigt), `CMD`.

## Dateien
`Core/PlanetSpawner_Session.cs`, `Modules/PlanetSpawner_Main.cs` (Phasen), `PlanetSpawner_FileManager.cs` (Config),
`PlanetSpawner_Logger.cs`, `PlanetSpawner_ModuleManager.cs`.

## Offene Punkte / Roadmap (aus `0_MOD Roadmap.md`)
- [ ] Commands ausbauen
- [ ] Planeten-Spawn **während der Laufzeit**
- [ ] Eventuell automatische Erkennung von Mod-Planeten (Phase 0 trägt sie schon mit `Enable=false` ein)
- [ ] Angleichen an `0_Phantombite_MOD_TEMPLATE.md` (thumb.jpg)
