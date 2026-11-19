---
title: Lokale Art-Pipeline für Cinderreach
slug: cinderreach-local-art
domains: [art, unity, gpu]
status: planned
branch: feature/cinderreach-local-art
repo_main: main
tickets: none
goal: cinderreach-local-art
health_goals: none (Einmaliger Produktionspilot mit GPU-Messung und Unity-Build-Abnahme.)
---

# cinderreach-local-art — Implementierungsplan

[Goal und Abnahmekriterien](../../goals/cinderreach-local-art/GOAL.md). Basis ist `e8a8b81`. S1–S4 sequenziell im Unity-Sandbox-Worktree ausführen. Vorab aktive Pläne zu Itemisierung und Party-Rollen bei der Integration berücksichtigen. Auf dem Host wurden 16.303 MiB VRAM gemessen, davon rund 2–3 GiB frei bei laufendem llama-server.

## File Structure

| Pfad | Verantwortung |
|---|---|
| `docs/world-art/local-art-pilot/README.md` und `workflows/{concept,prop}.json` darunter | Anleitung, Herkunft, Messwerte, Entscheidung und reproduzierbare ComfyUI-Exporte |
| `sandbox/Assets/Art/LocalArtPilot/` | `LootChest.glb`, `TornBanner.glb`, `Quadruped.fbx`, `LootIcon.png`, Materialien, Animationen und drei Wrapper-Prefabs unter `Prefabs/`, jeweils mit Unity-Metadaten |
| `sandbox/Packages/{manifest,packages-lock}.json` | glTFast und durch Unity aufgelöste Paketversion |
| `.gitattributes` | Git LFS für GLBs im Pilotordner |
| `sandbox/Assets/Editor/{SandboxBuilder,OssuaryBuilder}.cs` | Kandidaten für die Einbindung beim Szenenneuaufbau. Zuständigen Pfad vor Änderung im Codegraph bestätigen |
| `.agents/goals/cinderreach-local-art/PROGRESS.md` | Belegter Fortschritt |

Modellgewichte, Caches, verworfene Varianten und Builds lokal außerhalb des Git-Index halten. Freigegebene Exporte und erforderliche Blender-Quellen mit Git LFS verwalten.

## S1 – Lokale Generierung und Messung

- ComfyUI mit Blackwell-kompatiblem PyTorch isoliert einrichten. Werkzeug- und Modellrevisionen festhalten. FLUX.2 [klein] 4B FP8 bei Batch 1 und 768–1024 px für neutrale Einzelobjektreferenzen und Loot-Icon verwenden. Nativen TRELLIS.2-INT8-Workflow laden. Referenzquellen und Bedingungen aller verwendeten Gewichte einschließlich DINOv3 dokumentieren. [FLUX](https://docs.comfy.org/tutorials/flux/flux-2-klein) · [TRELLIS.2](https://docs.comfy.org/tutorials/3d/trellis2)
- Dienstbesitzer und Health-Endpunkt des llama-server ermitteln. Ausgangszustand erfassen, Wiederherstellung bei Ende, Fehler und Signal absichern, Dienst kontrolliert stoppen und VRAM-Freigabe prüfen. Nach dem GPU-Fenster Dienstzustand und Erreichbarkeit bestätigen.
- Je Versuch Asset, Seed, Auflösung, Dauer und Ergebnis erfassen. `nvidia-smi --query-gpu=timestamp,memory.used,memory.free --format=csv -lms 200` vom Leerlauf bis zum Export protokollieren. Baseline, höchstes Sample und Kaltstartzeit separat erfassen. Bei OOM Form-/Bake-Auflösung senken und bei Bedarf TripoSR mit Texturbake testen. Der TRELLIS.2-Nachweis bleibt bei erfolgreichem Fallback offen.
- **Abnahme:** Die gespeicherten Workflows lassen sich wieder öffnen. Konzept, Loot-Icon, texturiertes TRELLIS.2-GLB und Wiederherstellung des Dienstes sind belegt.

## S2 – Statische Prefabs

- Truhe in Blender aufbereiten. Für kleine Props 1.000–5.000 Dreiecke als Startbudget verwenden und Pivot, Maße, Silhouette und UVs prüfen. Bei Bedarf Banner als Stoffmesh mit lokaler Textur und expliziter Alpha-Maske exportieren. In URP Alpha Clipping verwenden.
- glTFast hinzufügen. Die beiden GLBs als visuelle Quellen der Prefabs einbinden. Truhe und Banner über den zuständigen Szenenaufbau platzieren. `SandboxBuilder.BuildDiabloZone` und `OssuaryBuilder.BuildOssuary` sind im Graphen identifiziert. Loot-State und Collider an der vorhandenen Spielobjektwurzel belassen. Icon am bestehenden sichtbaren Loot-Pfad anbinden.
- **Abnahme:** Szenenregeneration erhält die Platzierung, die Truhe liefert Loot und das Banner zeigt seine Alpha-Kanten vor hellem und dunklem Hintergrund. `python3 scripts/check-meta.py` endet mit Exit 0.

## S3 – Quadruped

- Lokalen Entwurf in Blender retopologisieren und riggen. Gewichte normalisieren, mit vier Einflüssen je Vertex beginnen. Lauf- und Angriffsclip als FBX mit Generic Rig importieren.
- Das Modell als visuelles Kind einer bestehenden Gegnerinstanz anbinden. Animator ohne Root Motion über bestehende Bewegungs- und EnemyAttack-Übergänge ansteuern. Konkrete Hooks vor Änderung am Aufrufpfad bestätigen.
- **Abnahme:** Im PlayMode und aus der Spielkamera Lauf, Angriff, Fußkontakt und sichtbare Deformation prüfen. Bestehende Kampfkomponenten steuern weiterhin Treffer und Loot.

## S4 – Build und Abschluss

- `python3 scripts/check-meta.py`, `bash scripts/run-unity-tests.sh` für EditMode und PlayMode sowie `python3 ~/scripts/agent-workspace.py check` ausführen. Linux-Player über `bash scripts/run-unity-build.sh` mit vorhandenem Cinderreach-Client-Einstieg bauen. ExecuteMethod und Erfolgsmarker im aktuellen Builder bzw. Runbook abgleichen.
- Den erzeugten Player starten. Truhe und Banner aus der Spielkamera aufnehmen. Beim Gegner Lauf und Angriff im Spiel aufzeichnen. Pro Asset Spitzen-VRAM, Versuche, Generierungszeit, manuelle Nacharbeit und Gesamtzeit im Pilot-README festhalten. Build und Aufnahmen mit Revision referenzieren. Geschätzte Cloud-Einsparungen als Schätzung ausweisen.
- Nach belegten Kriterien `PROGRESS.md` auf 100 % und offene Punkte auf `Keine.` setzen. `node ~/scripts/goal-lint.mjs --repo "$PWD"` und `python3 ~/scripts/plan-goal-gate.py .agents/plans/cinderreach-local-art/tasks.md` ausführen. Gezielten Commit über `python3 ~/scripts/agent-workspace.py land` abschließen.
