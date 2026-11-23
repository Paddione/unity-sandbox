---
goal: cinderreach-accounts
slug: cinderreach-accounts
health_goals: [G-DIABLO02, G-DIABLO03]
plan_refs: [.agents/plans/cinderreach-accounts/tasks.md]
---

# Cinderreach – Spielerkonten, Namen und Cloud-Held

Ausgangslage: [CINDERREACH.md](../CINDERREACH.md). Eigner-Entscheidungen 2026-11-23: Anmeldung mit Benutzername und
Passwort über Unity Authentication, der Held liegt per UGS Cloud Save am Konto. Heute meldet sich jeder Start anonym mit
gelöschtem Cache an (neue Spieler-ID pro Start), Mitspieler haben keinen sichtbaren Namen, der Held liegt nur lokal.

## Objective

Spieler aus dem Internet erscheinen mit ihrem Namen. Wer ein Konto anlegt, meldet sich mit Benutzername und Passwort an
und findet seinen Helden auf jedem PC wieder. Gastspiel bleibt ohne Konto möglich.

## Erfolgskriterien

1. Identität und Namen: Pro Spielkopie ein eigenes Auth-Profil statt Cache-Löschen (Automation bekommt Wegwerf-Profile),
   Gäste behalten ihre anonyme Identität. Jeder Spieler hat einen Anzeigenamen (UGS Player Names, im Startbildschirm
   änderbar, serverseitig bereinigt). Namensschilder über allen Helden, Beitritts-/Abgangsmeldung mit Namen. Ein
   Relay-Koop-Lauf mit drei Prozessen zeigt auf jedem Prozess die Namen der anderen.
2. Konto: Startbildschirm-Panel „Account“ mit Anmelden, Konto anlegen, Abmelden. Ein Gast kann sein Konto anlegen und
   behält dabei seine Spieler-ID (`AddUsernamePasswordAsync`). Ein Lauf belegt Anlegen, Abmelden, Wiederanmelden mit
   derselben Spieler-ID. Voraussetzung: Provider „Username & Password“ im UGS-Projekt aktiv.
3. Cloud-Held: `SaveService`-Schreibvorgänge laden den Helden zusätzlich in Cloud Save (Schlüssel `hero`, Hülle mit
   Version, Zeitstempel, Besitzer). Beim Anmelden entscheidet `CloudSaveResolver` (rein, getestet): hochladen,
   herunterladen, nichts oder den Spieler fragen (Konto-Held gegen PC-Held). Offline bleibt der lokale Stand nutzbar.
   Automation, Server und `-save-file` synchronisieren nie. Ein Lauf mit zwei Profilen („zwei PCs“) belegt, dass der
   Held auf dem zweiten Profil ankommt.
4. Abnahme: Windows-Client meldet sich an, spielt Koop mit einem zweiten Client (Namen sichtbar), Held kommt nach
   Neuinstallation aus der Cloud zurück. EditMode/PlayMode grün, Autoplay grün, Installer neu, Runbook-Abschnitt
   „Konten“, Ist-Stand eingearbeitet.

## Nicht-Ziele

Serverseitige Validierung des Cloud-Helden (der Client bleibt Autor seines Helden wie heute), Passwort-Zurücksetzen
per E-Mail, Freundeslisten, Steam-Anmeldung, Konto-Löschung im Spiel, Moderation von Namen über eine Grundbereinigung
hinaus.
