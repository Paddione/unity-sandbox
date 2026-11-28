# Progress: cinderreach-hero-boss-redesign

Prozent: 20 %

## Erledigt
- 2026-11-28: Kriterium 1 — Beleg: `~/scripts/blender-mcp-host.sh --headed` (dotfiles `8557bc4`), MCP `get_scene_info`/`execute_blender_code` steuern die Szene, Eigner bestätigt das sichtbare Fenster („ja go on“). Screenshot aus Blender heraus ist unter WSLg schwarz, daher Eigner-Sichtbestätigung als Nachweis.

## Nächste Schritte
- Kriterium 2 teilweise: Fenwalker v2 und Lanternbearer v1 vom Eigner in Blender freigegeben (2026-11-28, „awesome“), Renders `docs/hero-boss-redesign/01_fenwalker_v2.png`, `02_lanternbearer_v1.png`. Rogue v1 (`03_rogue_v1.png`) wartet auf Freigabe.
- Danach Bosse Kilnmother, Glasswright, Thewes (Kriterium 3).
- Builder-Skripte: `art-src/hero-boss-redesign/` (`hero_kit.py`, `fenwalker.py`, `lanternbearer.py`, `rogue.py`); Renders per `hero_kit.render_turn` (Cycles CPU, EEVEE liest unter WSLg schwarz zurück).

## Offene Punkte
- Kriterien 2–5 offen.
