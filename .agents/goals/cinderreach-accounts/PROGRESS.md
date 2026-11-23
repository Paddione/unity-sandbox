# Fortschritt – cinderreach-accounts

Prozent: 100 %

## Erledigt

- Provider „Username & Password“ vom Eigner aktiviert (2026-11-23, Probe liefert `WRONG_USERNAME_PASSWORD`).
- Vorprüfung 2026-11-23: Cloud Save und Player Names im UGS-Projekt `02e05e98-…` erreichbar (HTTP 200), Provider
  „Username & Password“ noch nicht aktiv (`PERMISSION_DENIED`).

- S1 Identität und Namen: `PlayerAccount` (Profile statt Cache-Löschen, Gast bleibt, `-profile`, `-player-name`),
  `PlayerNames.Sanitize`, `DiabloNetworkPlayer.playerName` (Server bereinigt), `HeroNameplates`, Beitritts-/Abgangszeilen,
  Startbildschirm „Playing as <Name>“ mit `AccountPanel` zum Umbenennen. Relay-Trio (`COOP_RELAY=1 COOP_CLIENTS=2`):
  Host, Client1, Client2 durchlaufen die volle Route (Exit 0, 0 Exceptions), jeder Prozess meldet die beiden anderen
  Namen („Client1 joined the party“ usw.). Nebenbefund behoben: der Relay-Host-Autoplay steuerte den abgetretenen
  Solo-Helden. EditMode 448/448, PlayMode 284/284.

- S2 Konto: `PlayerAccount.SignUpAsync` (Gast hängt Benutzer/Passwort an, Spieler-ID bleibt), `SignInAsync`,
  `SignOutAsync` (weiter als neuer Gast), `AccountRules` (UGS-Regeln, Fehlertexte), `AccountPanel` mit Anmelden,
  Konto anlegen, Abmelden. Live-Probe `scripts/account-smoke.sh` gegen das UGS-Projekt (Lauf 2026-11-23, Benutzer
  `crsmoke_1795459582`, danach gelöscht): 16/16 ok, u. a. Gast behält ID, Neustart stellt Konto und Namen wieder her,
  falsches Passwort und belegter Name mit Klartext, Wiederanmeldung liefert dieselbe Spieler-ID. EditMode 466/466,
  PlayMode 284/284.

- S3 Cloud-Held: Paket `com.unity.services.cloudsave` 3.4.2, `CloudHeroResolver` (rein: None/Upload/Download/Ask/
  StartFresh), `CloudHeroSync` (Schlüssel `hero`, Hülle `{v, savedTicks, owner, save}`, Side-Car `<save>.meta.json`,
  Upload 2 s nach jedem lokalen Schreiben, Abgleich bei jedem Kontowechsel, ersetzte Helden als `.bak`),
  Startbildschirm-Dialog „WHICH HERO?“. Live-Probe `account-smoke.sh` 2026-11-23 (Benutzer `crsmoke_1795460655`,
  danach gelöscht): 3 Läufe, 0 Fehler, u. a. Upload auf PC 1, Gast-Held gegen Konto-Held fragt auf PC 2, Konto-Held
  kommt an (Stufe 7, 123 Gold, Rogue), PC-Held als `.pc-hero.bak` gesichert. EditMode 479/479, PlayMode 284/284.

- S4 Abnahme: lokaler Relay-Server-Lauf mit Namen grün; Server-Image `ghcr.io/paddione/cinderreach-server@sha256:a6f619ab…`
  (`dc62621`), dotfiles `4dae8d16`, Pod bereit und veröffentlicht. Windows-Client `Games\cinderreach` (`dc62621`):
  Konto-Probe create/restore/pc2 je 0 Fehler (Held kommt nach „Neuinstallation“ = frischem Profil ohne lokalen Save
  aus der Cloud), Produktivlauf gegen den Pod mit WinTester (Lead) und LinuxTester (Follow), beide Namen sichtbar,
  0 Exceptions. Installer `Cinderreach-Setup-0.1.0-dc62621.exe`, Runbook „Konten, Namen und Cloud-Held“,
  `docs/coop-run-log.md` Abschnitt „Spielernamen und Konten“.

## Nächste Schritte

Keine.

## Offene Punkte

Keine.
