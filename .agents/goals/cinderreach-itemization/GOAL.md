---
goal: cinderreach-itemization
slug: cinderreach-itemization
health_goals: [G-DIABLO02, G-DIABLO03, G-DIABLO06]
plan_refs: [.agents/plans/cinderreach-itemization/tasks.md]
---

# Cinderreach – Itemisierung, Rollen-Uniques und Loot-Präsentation

## Objective

Ausrüstung skaliert mit dem Fortschritt durch die Gebiete und unterstützt unterschiedliche Builds. Drops zeigen ihre eigene Grafik und Seltenheit. Im Inventar können Spieler Fundstücke vergleichen, ausrüsten, beim Händler verkaufen oder wegwerfen.

## Erfolgskriterien

1. Das Itemlevel eines Drops stammt von der Stufe seiner Quelle. Jeder würfelbare Ausrüstungswert hat pro Itemlevel-Bereich definierte Minima und Maxima. Tests belegen Bereichsgrenzen, Unabhängigkeit von der Heldenstufe und den Erhalt gewürfelter Werte nach dem Laden.
2. Krit-Wertung erhöht die Chance auf kritische direkte Schadenstreffer. Erhöhter Flächeneffekt vergrößert die Wirkflächen ausgewiesener Schadens-, Heil- und Kontrollfähigkeiten. Trefferprüfung und Darstellung verwenden dieselbe Skalierung. Tests belegen kritischen Schaden und Zielerfassung an der Flächengrenze in Solo und Koop.
3. Für Fenwalker-Tank, Lanternbearer-Heiler/Support und Rogue-DPS gibt es je mindestens eine regulär erhältliche Unique-Waffe, -Rüstung und einen Unique-Helm. Jedes dieser Stücke hat Werte aus der höchsten für sein Itemlevel vorgesehenen Wertestufe und einen eigenen wirksamen Modifier. Alle Klassen können es tragen. Der Modifier greift über die jeweilige Fähigkeit oder Rollenmechanik. Bestehende Uniques dürfen eine Kombination erfüllen. Tests decken alle neun Kombinationen sowie Anlegen, Ablegen und erneutes Laden ab.
4. Jeder Ausrüstungsdrop zeigt die Grafik des konkreten Gegenstands. Magic leuchtet blau, Rare gelb und Unique orange. Spielaufnahmen zeigen alle drei Slots und Seltenheiten mit erkennbarer Grafik und Leuchteffekt. Ein Koop-Test belegt dieselbe Grafik und Seltenheit für denselben Drop auf Host und Client.
5. Die Gegenstandsansicht zeigt Name, Grafik, Slot, Seltenheit, Itemlevel, Rolls und Unique-Effekt mit klarer visueller Hierarchie. Der Vergleich mit dem ausgerüsteten Stück hebt Werteänderungen hervor. Ausrüsten, Verkauf beim Händler und Wegwerfen sind klar unterscheidbar und aktualisieren den betroffenen Zustand unmittelbar. Beim erneuten Aufheben bleiben Rolls und Modifier erhalten. Spielaufnahmen und Interaktionstests belegen die Ansicht und die drei Aktionen.
6. Bestehende Spielstände laden weiter und Ausrüstung behält ihre bisherigen Werte. Neue Rolls und Modifier bleiben in Inventar, Ausrüstung und Lager erhalten. Koop-Drops, Kampfvalidierung und Besitzwechsel sind host-autoritativ. Unity EditMode und PlayMode, Asset-Metadatenprüfung, Szenen- und Build-Verifikation, Akt-I-Autoplay und die deklarierten Health-Gates sind grün.

## Scope und Nicht-Ziele

Ausrüstung, Dropquellen, Kampfwerte, Rollen-Modifier, Loot-Darstellung, Inventar, Händleraktionen und die zugehörige Spielstand- und Koop-Integration. Die Unique-Matrix umfasst Waffe, Rüstung und Helm. Verbrauchsgegenstände folgen ihrer eigenen Progression. Weitere Slots, Setboni und ein neuer Handwerkskreislauf liegen außerhalb des Umfangs.

## Abhängigkeiten

- [Party Roles](../cinderreach-party-roles/GOAL.md) liefert den Rollenvertrag. Die Rogue-Modifier und ihre Nachweise benötigen die spielbaren Rogue-Fähigkeiten.
- [Build Uniques](../cinderreach-build-uniques/GOAL.md) liefert vorhandene Unique-IDs und Effekte zur Wiederverwendung. Vorhandene Item-Grafik und Mengen- und Seltenheitsgewichte sind der Ausgangspunkt. Begründe Änderungen an der Verteilung mit der Erreichbarkeit der Matrix.
