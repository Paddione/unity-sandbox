# Fortschritt – cinderreach-local-art

Prozent: 100 %

## Erledigt

- S1 ABGESCHLOSSEN (Belege in docs/world-art/local-art-pilot/README.md
  Abschnitte 1–5 + workflows/*.json): FLUX.2-klein 4B FP8 (Truhe s7 55,6 s /
  Banner s11 12,8 s / Hund s21 13,6 s / Icon s31 6,8 s, cfg 5, 20 Steps);
  TRELLIS.2 INT8 GLBs texturiert (Truhe 699k Tris/seed 101/1391 s,
  Hund 700k Tris/seed 202/272 s warm); VRAM-Peaks 15517/15970/11833 MiB
  (Budget 16.303); re-queuebare API-Workflows (concept.json 13 Nodes,
  prop.json 37 Nodes); llama :8090/:18340/:1919 kontrolliert gestoppt,
  VRAM-Fenster genutzt und ALLE DREI VERIFIZIERT WIEDERHERGESTELLT
  (Health 200, exakte Start-Cmds, UUID-Masken-Vorfall dokumentiert).
- S2 ABGESCHLOSSEN: Truhe→3k Tris/4,3 MB (Pivot Boden, 1 m);
  Banner→Stoffplane 475 Tris + Alpha-Maske MASK/Cutoff 0,5, Bracket +1,9 m;
  glTFast 6.21.0; Wrapper-Prefabs + persistente URP/Lit-Materialien;
  Verifikation: check-meta OK, VerifyDiabloZone OK, EditMode 344/344.
- S3 ABGESCHLOSSEN: Hund→12k-Tris-Retopo + FK-Rig (20 Bones,
  Envelope-Weights max 4 Einflüsse) + Run(24f)/Attack(20f) NLA-Clips,
  FBX Generic; als Kind von Fen Brute (Mimic spiegelt
  moving/attackCount/IsAlive, kein Root Motion); Kampfkomponenten unberührt.
- S4 ABGESCHLOSSEN: Linux-Player-Build erfolgreich (140 MB, ~184 s,
  Marker `DiabloZone build result: Succeeded` belegt); 15 Spielkamera-Shots
  (`-pilotshots -class fenwalker`, Exit 0) in
  docs/world-art/local-art-pilot/shots/ (Truhe+Icon, beide Banner mit
  Alpha-Kante, Hund idle/Run/Attack + echter Aggro-Take mit
  Fen-Brute-Leiste/Treffer/Loot); Produktionsentscheidung + Bilanz in
  README Abschnitte 5/8; EditMode 344/344 + PlayMode 204/204 grün.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
