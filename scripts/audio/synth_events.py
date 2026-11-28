"""Deterministic procedural synth for 6 game SFX event cues.

Pure stdlib (math only). No randomness. Each cue: sum of sines with
per-partial exponential decay, per-note 5ms raised-cosine attack, one-pole
lowpass, normalized to peak 0.7. Returns (samples, 44100).
"""
from __future__ import annotations

import math

SR = 44100
PEAK = 0.7
ATTACK_S = 0.005

CUTOFFS = {
    "LevelUp": 1300,
    "Gate": 850,
    "Heal": 1200,
    "Quest": 1200,
    "RareDrop": 1300,
    "Shrine": 1100,
}

# (freq, start_s, tau_s, amp, partials[(ratio, amp_ratio, tau_scale)])
RECIPES: dict[str, list[tuple]] = {
    "LevelUp": [
        (146.83, 0.00, 0.38, 0.50, ((2.0, 0.25, 0.5), (3.0, 0.12, 0.3))),
        (220.00, 0.02, 0.38, 0.45, ((2.0, 0.25, 0.5), (3.0, 0.12, 0.3))),
        (293.66, 0.04, 0.42, 0.50, ((2.0, 0.30, 0.5), (3.0, 0.12, 0.3))),
        (369.99, 0.06, 0.42, 0.42, ((2.0, 0.30, 0.5), (3.0, 0.12, 0.3))),
    ],
    "Gate": [
        # sub rumble sweep handled specially (freq glide 65->98Hz); chord bed below
        (146.83, 0.05, 0.45, 0.40, ((2.0, 0.20, 0.5),)),
        (220.00, 0.05, 0.45, 0.36, ((2.0, 0.20, 0.5),)),
        # heavy low thud near 0.9s
        (55.00, 0.90, 0.12, 1.00, ((2.0, 0.30, 0.5), (3.0, 0.10, 0.3))),
    ],
    "Heal": [
        (293.66, 0.00, 0.38, 0.60, ((2.0, 0.30, 0.5), (3.0, 0.12, 0.3))),
        (369.99, 0.12, 0.38, 0.55, ((2.0, 0.30, 0.5), (3.0, 0.12, 0.3))),
        (440.00, 0.24, 0.42, 0.55, ((2.0, 0.30, 0.5), (3.0, 0.12, 0.3))),
    ],
    "Quest": [
        (220.00, 0.00, 0.30, 0.60, ((2.0, 0.25, 0.5),)),
        (293.66, 0.25, 0.30, 0.60, ((2.0, 0.25, 0.5),)),
        (329.63, 0.50, 0.30, 0.60, ((2.0, 0.25, 0.5),)),
        (293.66, 0.75, 0.34, 0.60, ((2.0, 0.25, 0.5),)),
    ],
    "RareDrop": [
        (220.00, 0.00, 0.40, 0.55, ((2.0, 0.35, 0.5), (3.0, 0.15, 0.3))),
        (293.66, 0.15, 0.40, 0.55, ((2.0, 0.35, 0.5), (3.0, 0.15, 0.3))),
        (329.63, 0.30, 0.40, 0.55, ((2.0, 0.35, 0.5), (3.0, 0.15, 0.3))),
        (440.00, 0.45, 0.45, 0.55, ((2.0, 0.35, 0.5), (3.0, 0.15, 0.3))),
    ],
    "Shrine": [
        (261.63, 0.00, 0.55, 0.60, ((2.0, 0.35, 0.5), (2.76, 0.15, 0.3))),
        (196.00, 0.00, 0.60, 0.45, ((2.0, 0.25, 0.5),)),
        (523.25, 0.00, 0.35, 0.20, ()),
    ],
}

DURATIONS = {
    "LevelUp": 1.6,
    "Gate": 1.6,
    "Heal": 1.5,
    "Quest": 1.6,
    "RareDrop": 1.6,
    "Shrine": 1.6,
}


def _render(key: str) -> list[float]:
    dur = DURATIONS[key]
    n = int(round(dur * SR))
    out = [0.0] * n
    for freq, start, tau, amp, partials in RECIPES[key]:
        s0 = int(round(start * SR))
        # fundamental + partials
        comps = [(1.0, 1.0, 1.0)] + [(r, ar, ts) for r, ar, ts in partials]
        for ratio, aratio, tscale in comps:
            f = freq * ratio
            t = tau * tscale
            a = amp * aratio
            for i in range(s0, n):
                tn = (i - s0) / SR
                env = math.exp(-tn / t)
                if tn < ATTACK_S:
                    env *= 0.5 * (1.0 - math.cos(math.pi * tn / ATTACK_S))
                out[i] += a * env * math.sin(2.0 * math.pi * f * tn)
    if key == "Gate":
        # sub rumble glide 65->98Hz over 0.85s, tau decay
        s0 = 0
        nsub = min(n, int(round(0.85 * SR)))
        for i in range(s0, nsub):
            tn = (i - s0) / SR
            f = 65.0 + (98.0 - 65.0) * (tn / 0.85)
            env = math.exp(-tn / 0.35)
            if tn < ATTACK_S:
                env *= 0.5 * (1.0 - math.cos(math.pi * tn / ATTACK_S))
            # phase-integrated glide: phase = 2*pi*(f0*t + k/2 t^2)
            k = (98.0 - 65.0) / 0.85
            phase = 2.0 * math.pi * (65.0 * tn + 0.5 * k * tn * tn)
            out[i] += 0.65 * env * math.sin(phase)
    # one-pole lowpass
    fc = CUTOFFS[key]
    a = 1.0 - math.exp(-2.0 * math.pi * fc / SR)
    y = 0.0
    for i in range(n):
        y += a * (out[i] - y)
        out[i] = y
    # normalize to peak 0.7
    peak = max((abs(x) for x in out), default=0.0)
    if peak > 1e-12:
        g = PEAK / peak
        out = [x * g for x in out]
    return out


def synth_event(key: str) -> tuple[list[float], int]:
    """Return (mono float samples, sample rate) for one of 6 event cues."""
    if key not in RECIPES:
        raise KeyError(f"unknown event cue {key!r}; expected one of {sorted(RECIPES)}")
    return _render(key), SR
