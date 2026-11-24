# Cinderreach-Asset-Audit und Ausrüstungsmodelle

health_goals: [G-DIABLO01, G-DIABLO02]

## Objective

Alle im Spiel verwendeten Unity-Assets inventarisieren. Spielbare Basischaraktere tragen leichte Kleidung und gelbe Rüstungsteile mit dezenten Klassenunterschieden. Jedes Unique in einem am Charakter sichtbaren Slot erhält ein eigenes 3D-Modell.

## Erfolgskriterien

- Inventar mit Herkunft und Einsatzort, einschließlich Szenen, Prefabs, transitiver Abhängigkeiten und zur Laufzeit geladener Assets. Offene Zuordnungen dokumentieren.
- Jede Klasse mit Standardausrüstung aus der regulären Spielkamera belegen.
- Jedes sichtbare Unique über seine stabile Item-ID einem geometrisch unterscheidbaren Modell zuordnen und aus der Spielkamera belegen. Anlegen, Wechseln und Ablegen prüfen.
- Die Modellzuordnung im Unity-Verifikationspfad von G-DIABLO02 dauerhaft gegen den aktuellen Item-Katalog prüfen. Fehlende, ungültige und doppelt verwendete Modelle müssen den Check fehlschlagen lassen.
- Asset-Metadatenprüfung und relevante EditMode- und PlayMode-Tests bestehen.

## Nicht-Ziele

Das Audit umfasst alle verwendeten Assets. Welt- und Gegnerassets, UI, Itemwerte und Spiellogik liegen außerhalb der Umsetzung. Eigene oder bereits freigegebene Assets verwenden. Asset-Store-Käufe erfordern Freigabe. Bestehenden Item-Katalog und die Spielkamera für die Abnahme verwenden.
