# Phantombite PlanetSpawner

Spawnt **Mod-Planeten** (z. B. Sulvax, Pandora) beim Serverstart an festen Koordinaten, falls sie in der Welt fehlen. Neue
Mod-Planeten werden automatisch mit `Enable=false` in die Config eingetragen und müssen nur noch mit Koordinaten aktiviert werden.

## Funktionen
- Erkennt alle geladenen Planeten-Definitionen (ohne Vanilla) und trägt neue in die Config ein
- Prüft, ob ein Planet in der Welt schon existiert, und spawnt nur fehlende
- Gültigkeitsprüfung der Config (aktiv, Typ, Durchmesser, Seed, Definition vorhanden)

## Commands (nur Admin)
```
!pbc planetspawner <command>
```
| Command | Beschreibung |
|---|---|
| `check` | Planeten prüfen und fehlende spawnen |
| `list` | Alle konfigurierten Planeten auflisten |

## Konfiguration
`PlanetSpawner_Config.ini` im World-Storage. Pro Planet: `Enable`, `SubtypeId`, `Diameter`, `Seed`, `PosX`, `PosY`, `PosZ`.
Einträge ohne Koordinaten werden als „Koordinaten fehlen!“ gemeldet.

## Voraussetzungen
- **Phantombite Core** (Commands)

Workshop-ID: 3723481681
