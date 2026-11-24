# Progress: cinderreach-voice-batch

Prozent: 0 %

## Erledigt

- 2026-11-24: setgoal-Runde beantwortet (Umfang = nur Stale, GPU = CUDA mit Auto-Restore,
  Stimmen = 8 Bestand, Ablage = S2-Ausführung von `cinderreach-sprache-voice`).
- 2026-11-24: Goal persistent angelegt (GOAL.md + PROGRESS.md, health_goals ab 0 %) und
  Ausführungsplan `.agents/plans/cinderreach-voice-batch/tasks.md` geschrieben.

## Nächste Schritte

- B1: Preflight (Stale-Inventur, Inkling-Status, VRAM-Belegung) — erst auf Eigner-Go.
- B2: Batch (Inkling pausieren, CUDA-Render, Lock fortlaufend sichern, Inkling restarten + Probe).
- B3: Abnahme (QA-Transkription, Voice-Gates, Suiten, Run-Log + Doku).

## Offene Punkte

- Eigner-Go für das GPU-Fenster steht aus (Inkling-Pause auf der 5070 Ti).
- Textstände aus Akt-I/II/III-S1 müssen vor B2 stehen (gestaffelt via `cinderreach-sprache-voice` S2).
