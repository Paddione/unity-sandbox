---
health_goals: none (Einmaliger Produktionspilot mit GPU-Messung und Unity-Build-Abnahme.)
plan_refs: [.agents/plans/cinderreach-local-art/tasks.md]
---

# Lokale Art-Pipeline für Cinderreach

## Objective

Auf der RTX 5070 Ti eine reproduzierbare lokale Art-Pipeline einrichten und drei Assets bis in den spielbaren Cinderreach-Build bringen. Pro Assetklasse anhand von Zeit und Qualität entscheiden, ob lokale Produktion genügt.

## Erfolgskriterien

1. FLUX.2 [klein] 4B FP8 erzeugt Konzepte und ein Loot-Icon. Natives TRELLIS.2 INT8 erzeugt ein texturiertes GLB. Workflows, Seeds, Modellversionen, Referenzquellen, Lizenzen und Startanleitung sind dokumentiert.
2. Loot-Truhe und ausgefranstes Banner sind als statische Prefabs mit korrektem Maßstab, Pivot, Materialien und erforderlichen Collidern eingebunden. Die Bannerkante ist im URP-Build sichtbar.
3. Der vierbeinige Gegner entsteht aus einem lokalen Entwurf, wird in Blender retopologisiert und geriggt und als FBX Generic importiert. Lauf und Angriff funktionieren ohne grobe sichtbare Deformationsfehler.
4. Pro Asset sind Spitzen-VRAM, Versuche, Generierungszeit, Nacharbeit und Gesamtzeit bis zum brauchbaren Prefab erfasst. Build, Spielkamera-Aufnahmen und Produktionsentscheidung sind dokumentiert.

## Randbedingungen und Nicht-Ziele

2D und 3D sequenziell ausführen. Der llama-server darf vorübergehend kontrolliert gestoppt werden. Einen zuvor laufenden Dienst auch bei Fehler oder Abbruch wieder starten und seine Erreichbarkeit prüfen. Serienproduktion, bezahlte Cloud-Läufe und dauerhafte Health-Probes liegen außerhalb des Piloten.
