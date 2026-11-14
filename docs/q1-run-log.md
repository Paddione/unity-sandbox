# Q1 run log — 2026-11-14 (headless)

Build: `SandboxBuilder.BuildAll` (Editor 6000.0.85f2, batchmode, nographics).
Result: `Succeeded in 21.6s, 91 MB` → `builds/greybox/greybox` (gitignored proof artifact).

Verify: `SandboxBuilder.VerifyGreybox` (batchmode):

```text
[SandboxBuilder] Verify: player=True controller=True blocks=6 walls=4 triggerZones=1 orbs=3 goalPads=1
[SandboxBuilder] Verify: OK (Q1 rig + Q2 trigger + Q3 toy/goal present).
```

Player run: `./greybox -batchmode -nographics`, 40 s, then stopped.
Shutdown sequence clean (`Physics::Module` cleanup, Input System
`ShutdownInProgress → Shutdown`, no exceptions, no game errors).
Only noise: ALSA/FMOD `failed to initialize the output device` —
headless machine has no sound card (same as foundation proof).

Input path: bindings are code-defined in `ThirdPersonController`
(`Keyboard`/`Mouse` via the Input System package, no `.inputactions`
asset); headless has no keyboard/mouse, so scripted key injection is
not possible. Smoke test asserts the Input System initializes and the
build logs no input errors — live movement feel stays a human-playtest
follow-up (see `docs/playtest-protocol.md`).

Toy logic (`ToyTrigger`: counter / pickup ×3 / goal pad red→green) is
compiled into the build; entry/pickup/goal counting executes on live
player trigger contact (human playtest), not headless.
