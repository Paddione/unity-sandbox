# Goal: cinderreach-model-realism

health_goals: none (Einmalige künstlerische Überarbeitung des verwendeten Modellbestands.)
plan_refs: [.agents/plans/cinderreach-model-realism/tasks.md]

## Objective
Cinderreach erhält glaubwürdigere Formen und Oberflächen für die im Spiel sichtbaren Modelle. Aus der Spielkamera sollen Figuren, Ausrüstung und Umgebung anhand ihrer Silhouette und Materialwirkung erkennbar bleiben.

## Ist-Befunde
- B1 docs/cinderreach-asset-inventory.tsv führt BloodMoor-Requisiten, LocalArtPilot-Modelle und verwendete KayKit-Figuren, Waffen und Architektur mit Einsatzorten auf.
- B2 Das Inventar enthält auch Animationsquellen und ein Paket-Debugmesh. Die Dateiendung allein belegt keinen sichtbaren Einsatz.

## Entscheidungen
- D1 Alle verwendeten Modelle einschließlich Drittanbieter-Assets einbeziehen.
- D2 Realistischere Formen und Oberflächen sind die gewählte Bildsprache.
- D3 Vorher und nachher bei gleicher Kamera und Beleuchtung vergleichen, um Änderungen am Modell zu beurteilen.

## Erfolgskriterien
1. Die Abdeckung jedes verwendeten Modells ist nachvollziehbar, einschließlich zur Laufzeit erzeugter Modelle. Nachweis durch Zuordnung des Bestands zu Einsatzort und Bearbeitung.
2. Figuren, Ausrüstung und Umgebung zeigen zur jeweiligen Modellfamilie passende Verbesserungen. Nachweis durch vergleichbare Vorher-Nachher-Ansichten und Prüfung aus der Spielperspektive.
3. Die Modelle funktionieren im Spiel mit ihren bisherigen Referenzen und Animationen. Nachweis durch Unity-Import, passende Spieltests und Repository-Prüfungen.

## Nicht-Ziele
- Unbenutzte Bibliotheksmodelle, reine Animationsquellen und Editor-Debuggeometrie.

## Abhängigkeiten
- Modellquellen im Unity-Projekt und Unity-Editor gemäß sandbox/ProjectSettings/ProjectVersion.txt.
