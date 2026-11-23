---
title: "Cinderreach Konten, Namen, Cloud-Held"
domains: [unity, networking, services]
status: in_progress
goal: cinderreach-accounts
slug: cinderreach-accounts
branch: feature/cinderreach-accounts-sN
repo_main: main
tickets: none
health_goals: [G-DIABLO02, G-DIABLO03]
---

# cinderreach-accounts — Plan

[Ziel](../../goals/cinderreach-accounts/GOAL.md). Planbasis `bb8670c`.

## S1 · Identität und Namen

- Neu `Scripts/Net/PlayerAccount.cs`: einmalige UGS-Initialisierung mit Profil (`-profile <name>`, sonst `main`;
  Automation-/Koop-Läufe `auto<pid>` mit Cache-Löschen), Gast-Anmeldung, Anzeigename (`GetPlayerNameAsync`,
  `UpdatePlayerNameAsync`, `-player-name` für Läufe), `PlayerNames.Sanitize` (rein: 3–16 Zeichen, Buchstaben/Ziffern/
  Leerzeichen/`-_`, `#1234`-Suffix nur intern). `RelayJoin.EnsureUgsAsync` delegiert hierher.
- `DiabloNetworkPlayer`: `NetworkVariable<FixedString32Bytes> playerName`, Besitzer meldet per ServerRpc, Server bereinigt.
- Neu `Scripts/Net/HeroNameplates.cs`: Bildschirm-Labels über allen Netz-Helden (eigener in Gold, andere hell),
  Beitritts-/Abgangsmeldung über `DiabloJuice.ShowText`/Log.
- `ClassSelectUI`: Zeile „Playing as <Name>“ mit Umbenennen.
- Tests: EditMode `PlayerNamesTests` (Bereinigung, Profilwahl), PlayMode Nameplate; Koop-Relay-Trio mit
  `-player-name` belegt Namen in allen Logs.

## S2 · Konto mit Benutzername und Passwort

- `PlayerAccount`: `SignUpAsync`, `SignInAsync`, `SignOut`, Gast-Upgrade per `AddUsernamePasswordAsync`, Fehlertexte.
- Neu `Scripts/Menu/AccountPanel.cs`: Felder Benutzer/Passwort, Anmelden, Konto anlegen, Abmelden, Statuszeile.
- `scripts/ugs-account-probe.sh`: REST-Probe (Anlegen, Anmelden, Löschen eines Testkontos).
- Tests: EditMode Eingabeprüfung (UGS-Regeln: Benutzer 3–20, Passwort 8–30 mit Groß/Klein/Ziffer/Sonderzeichen).

## S3 · Cloud-Held

- Paket `com.unity.services.cloudsave` 3.4.2.
- Neu `Scripts/Save/CloudSaveResolver.cs` (rein) und `Scripts/Save/CloudHeroSync.cs` (Hülle `{v, savedAt, owner, save}`,
  Upload nach `SaveService.WriteSave` entprellt, Download/Abgleich nach Anmeldung, Besitzer-Datei neben dem Save).
- `ClassSelectUI`: „Syncing…“, Wahl-Dialog Konto-Held gegen PC-Held.
- Tests: EditMode `CloudSaveResolverTests` (alle Fälle), Zwei-Profil-Lauf.

## S4 · Abnahme

- Windows-Client: Anmelden, Koop mit Namen, Held nach Löschen des lokalen Saves aus der Cloud. Installer neu,
  `docs/runbook.md` Abschnitt „Konten“, CINDERREACH.md einarbeiten, Goal entfernen.
