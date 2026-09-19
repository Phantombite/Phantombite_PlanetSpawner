# Patch Notes — Phantombite_PlanetSpawner

## 2026-09-19 — Bereinigung
- Planetenerkennung genauer: vorhanden gilt nur bei exaktem Namen oder `Typ-...`, nicht mehr bei jedem Namen mit
  gleichem Anfang (`Sulvax` erkannte sonst auch `SulvaxMond` als vorhanden)
- Null-Prüfung für den Speichernamen (mögliche Absturzursache)
- Doku angelegt (`DEV_Funktion.md`), `.gitignore` ergänzt

## v1.1.0 — Mai 2026
- Template-Konformität hergestellt
- Config von XML (Data/Config/) auf WorldStorage FileManager umgestellt
- Ordnerstruktur bereinigt

## v1.0.0 — Initial
- Planet-Spawn via statischer XML Config
