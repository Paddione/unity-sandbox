# Runbook — Concept → Prototype → (optional) Vertical Slice

Chain for the mechanics testbed. Upper bounds only; each stage ends with
its exit question and the evidence listed below.

## 1. Concept (week 0–1)

- One-sentence core goal + One-Page GDD draft (MoSCoW, done-means, dates).
- Exit question: "is the concept testable as one greybox question?"
- Evidence: `docs/one-page-gdd.md` + `docs/mechanics-questions.md`.

## 2. Prototype (1–4 weeks per question)

- One prototype, one question (Q1 movement, Q2 trigger, Q3 toy/goal).
- Greybox discipline: primitives, fresh URP/Lit, code-owned rig,
  fake everything else.
- Exit question: **"is the core mechanic fun?"**
- Evidence per question: greybox scene + `VerifyGreybox` green +
  player build run log + one `docs/decision-log.md` line
  (continue / pivot / kill).

## 3. Vertical Slice — option only

- Starts only if the sandbox answers the exit question with fun
  (all three Q1–Q3 verdicts `continue`, loop readable).
- Not started; no slice work is part of this goal.
- Evidence: decision-log line explicitly opening the slice (absent).

## Server-Rebuild über das Agent-Board

- Der Unity-Tab im Agent-Board (`/unity`) baut den Dedicated Server neu und
  rollt ihn nach gpu-metal aus. Gleiches Kommando auf der Shell:

```
python3 ~/scripts/unity_ops.py rebuild --scene greybox
```

- Phasen: `stop-editor` (Dauer-Editor `unity-sandbox-editor.service` gibt die
  Projektsperre frei) → `build` (`SandboxBuilder.BuildServer -serverScene <scene>`)
  → `start-editor` (läuft auch nach einem Fehler) → `deploy` (`BUILD_INFO.json`
  plus Rsync nach `gpu-metal:~/greybox-server/`) → `restart`
  (`greybox-server.service`) → `verify` (`[Net] Server started` im frischen Log).
- Stand und Phase: `~/.local/state/unity-ops/rebuild.json`, Build-Log
  `~/.local/state/unity-ops/build.log`.
- `-serverScene diablo` wählt `Assets/Scenes/Diablo.unity`, jeder andere Wert
  oder kein Argument die Greybox. Die Szene steht in der Logzeile
  `Server build result: … (scene <Datei>)`.
