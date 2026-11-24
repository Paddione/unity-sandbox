# B1-Preflight — cinderreach-voice-batch

- Datum: 2026-11-24 (UTC)
- Branch: feature/cinderreach-voice-batch
- Worktree: /home/patrick/.worktrees/unity-sandbox-3c21c090/feature/cinderreach-voice-batch
- Task: cinderreach-voice-batch-B1-preflight-retry1 (Retry nach vLLM-ECONNREFUSED; kein GPU-Eingriff, kein Render, kein Service-Stopp)

## 1. check-voice-lines.py

Befehl: `python3 scripts/check-voice-lines.py`
Output:
```
voice lines: 115, problems: 0
```
Exit-Code: 0

Interpretation: Lock (`docs/audio/voice-lines.lock.json`, 115 Eintraege) ist konsistent mit `Voice/*.ogg` — keine Stale-/Missing-Keys zum Render-Zeitpunkt. Stale-Count: 0, Key-Liste: (leer).

## 2. VRAM-Stand (`nvidia-smi`)

- GPU: NVIDIA GeForce RTX 5070 Ti, 15046 MiB / 16303 MiB belegt (~92 %), Temp 39C, P5, 16W/300W
- Prozesse: /Xwayland (G), /strata-inkling (C, PID 1060955)
- Hinweis: VRAM fast voll durch Inkling-2-Mini (K8V4 KV on-card); TTS-Render (vLLM :8123/:8001) derzeit down — Cloud-Fallback fuer B2 einplanen, kein lokaler GPU-Eingriff in B1.

## 3. Inkling-Status + Health

- `systemctl --user status inkling2-mini-strata.service`: active (running) seit 2026-11-24 15:35:43 CET (12 min), Main PID 1060906 (strata serve :1919, Profil inkling-2-mini-1919.json, 131072 ctx, K8V4, Vision + DSpark)
- `curl -s -m 10 http://127.0.0.1:1919/health`:
```json
{"status": "ok", "max_context": 131072, "model": "inkling-2-mini-sq-10", "images": true, "api_key": false, "image_generation": false, "loaded": true, "service": "strata"}
```
Curl-Exit: 0

## 4. Lock-Uebersicht

- `docs/audio/voice-lines.lock.json`: dict mit 115 Eintraegen
- Beispiel-Eintrag `bark.abbot.death`: `{"hash": "6c602b907d5c", "speaker": "abbot", "seconds": 3.15}`
- `docs/audio/voices.json`: Top-Level-Keys [note, design_model, clone_model, pronounce, voices]; `voices` = 8 Stimmen: ottilie, ilsabe, jorin, merle, narrator, abbot, kilnmother, glasswright (Klon-Modell lt. Graph-Kontext: Qwen3-TTS-12Hz-0.6B-Base)

## 5. Go-Einschaetzung B2

- GO (mit Cloud-Fallback): Lock ist sauber (0 Probleme), Inkling :1919 healthy, keine B1-seitigen Blocker. vLLM :8123/:8001 down bestaetigt den Retry-Grund — B2-Render nur via Cloud-Fallback, kein lokaler vLLM-Restart aus diesem Paket.

## 6. Naechster Schritt B2

- B2-Renderpaket via Cloud-Fallback anstossen (kein lokales vLLM, kein Inkling-Stopp); Render-Log ausserhalb dieses Write-Sets fuehren (`docs/voice-batch-render.log`, `docs/voice-batch-run-log.md` bleiben unberuehrt).
