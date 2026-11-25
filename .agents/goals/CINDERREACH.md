# Cinderreach — Ist-Stand und Ausgangspunkt

Stand: 2026-11-22, `main` @ `c89dc40`. Dieses Dokument ersetzt alle abgeschlossenen Cinderreach-Goals,
ihre Stufenpläne und die alte `ROADMAP.md`. Neue Arbeit beginnt hier: neues Goal unter
`.agents/goals/<slug>/` anlegen und dieses Dokument als Ausgangslage referenzieren. Nach dem Abschluss
eines Goals seinen Stand hier einarbeiten und das Goal-Verzeichnis samt Plan entfernen.

Historie der konsolidierten Goals liegt in Git. Ein Abruf erfolgt per
`git log --oneline -- .agents/goals/<slug>` und `git show <sha>^:.agents/goals/<slug>/PROGRESS.md`.
Laufprotokolle bleiben unter `docs/` (siehe „Belege und Doku“).

## Aktive Goals

| Goal | Stand | Nächster Schritt |
|---|---|---|
| [`cinderreach-party-roles`](cinderreach-party-roles/GOAL.md) | S1–S4 auf `main` (`1842d1f`, `75995ce`, `e388d05`, `e8a8b81`), S5 läuft im Worktree `feature/cinderreach-party-roles-s5`. `PROGRESS.md` ist noch nicht nachgezogen. | S5 Koop-Gruppe, dann S6 Nachweis |
| [`cinderreach-itemization`](cinderreach-itemization/GOAL.md) | 50 %, S1–S3 auf `main` (`b079676`, `bf9db19`, `c94259d`) | S4 Rollen-Uniques (braucht Rogue-Skills aus Party Roles), S5 Präsentation, S6 Abnahme |
Bewusst offen ohne Goal: Eigner- bzw. 5-Personen-Playtest, Steam-Build.

Abgeschlossen: `cinderreach-character-creation` (2026-11-23): Charaktererstellung im `ClassSelectUI`
(Klasse → Gender/Frisur/Haarfarbe/Build/Name → Start) mit zufälligem deutschen Heldennamen als
Standard (Würfel + Editieren), rein kosmetischen Proportions-/Haar-Aufsätzen, Save v14 und
Koop-Sync (gepackter Look + Heldname im Nameplate vor dem Accountnamen); EditMode 504/504,
PlayMode 290/290. `cinderreach-deutsch-story` (2026-11-23, Belege `docs/story-run-log.md`). Das Spiel ist
vollständig deutsch (eigene Stringtabelle `Loc`, Englisch per `-lang en` als Fallback, Eigennamen laut
Glossar in `docs/story-bible.md`; nur der Produktname „Cinderreach“ bleibt). Akt I und II haben je eine
Hauptaufgabenkette (H1–H9) mit Nebenaufgaben (N1–N5) über NPC-Dialoge (Ottilie Wachs, Merle Sandkamp,
Ilsabe Krumm, Jorin Hagedorn), Fuhrbuch (Taste J), HUD-Zielzeile und Fundstücke; Queststand im Save v13,
im Koop per Host-Snapshot (Max-Merge) synchron.

Seit `cinderreach-story-voice` (2026-11-23): Jede Nebenaufgabe ändert eine Zeile der Hauptkette (Sprecher `x|nK`
bzw. `x|!nK`, `StoryDialog.Visible`), Bosse sprechen in Phase 2 und beim Tod, zwei Erzählzeilen markieren H4-Start
und Wendung 3 (`StoryBarks`, Untertitel über der Aktionsleiste). Alle 115 Dialog- und Zurufzeilen sind deutsch
vertont: acht synthetische Stimmen (`docs/audio/voices.json`, Qwen3-TTS VoiceDesign → Clone, lokal auf CPU),
Clips unter `Resources/Voice/<key>.ogg`, Wiedergabe `StoryVoice` mit Regler „Stimmen“. Gate
`scripts/check-voice-lines.py` (fehlende/veraltete Clips), Neurendern `scripts/voice/render_voices.py`,
Verständlichkeit `scripts/voice/qa_transcribe.py`. Koop: Boss-Zurufe hört sicher nur, wer Phase-2-/Todesereignis
lokal erhält (Host, Abt- und Glasmacher-Phase auch Clients).

Benutzen-Taste `T` (2026-11-24, `UseKey`): Zeigt „[T] …“ für das nächste nutzbare Objekt in Reichweite und
benutzt es wie ein Klick: Story-NPCs, Ilsabe (Gespräch, sonst Handel), Jorin (Gespräch, sonst Schmiede), Truhe,
Wegschrein, Wagnisschrein, Auftragsbrett, Beutekisten. Objekte implementieren `IUsable` und melden sich in
`Usables` an. Q/E/R/Leertaste/F/G bleiben Fertigkeitsplätze.

Geteilte Fortbewegung (2026-11-24, `LocomotionTwist`): Held, Koop-Spiegel und animierte Gegner drehen nach der
Animation die Hüfte (`hips`) in die gemessene Laufrichtung, `spine`/`chest` drehen zurück, sodass der Oberkörper
die Root-Ausrichtung behält (Held: Maus, Gegner: Held). Mehr als 100° gegen die Blickrichtung läuft rückwärts
(Float `MoveSpeed` = −1 als Zustands-Speed auf `Run`/`Walk`, Hysterese 80°). `Moving` gilt auch bei gemessener
Bewegung (Koop-Spiegel, Lanzer, Wirker). NPCs (`StoryNpc` mit Animator) drehen sich bei Klick oder `T` zum Helden.

## Spiel in einem Absatz

Cinderreach ist ein isometrisches Co-op-ARPG (Unity 6000.0.85f2, URP, `sandbox/`), Arbeitstitel und
`productName` „Cinderreach“. Eine Session führt von der Siedlung **Lantern Hold** durch **Ashen Fen**
und **Sunken Ossuary** (Boss **The Hollow Abbot**) in den **Cinder Kiln** (Akt-Boss **The Kilnmother**)
und schließt Akt I ab. Der Kilnmother-Kill öffnet Akt II mit den **Glass Barrens** (Zonen-Boss
**The Glasswright**). Spieler wählen eine von drei Klassen, sammeln Beute mit Affixen, schmieden,
nehmen optionale Aufträge an und spielen die Stufen Normal und Kindled. Solo füllt KI die Gruppe auf
drei Rollen auf. Im Koop spielen bis zu vier Spieler host-autoritativ über NGO. Interne Bezeichner
(`Diablo*`-Klassen, Szenendateien) bleiben bewusst unverändert, spielersichtbare Namen sind eigen
(`IdentityTests` sperrt Altnamen).

## Projektstruktur

- Szenen: `DiabloZone.unity` (das Spiel, alle Zonen als Roots in einer Szene, +200 m X Ossuary,
  +400 m X Kiln, +600 m X Glass Barrens), `Diablo.unity` (früher Prototyp), `Greybox.unity` (Mechanik-Testbed).
- Szenen nur über Builder regenerieren, nie von Hand: `SandboxBuilder.BuildDiabloZone` mit
  `TownBuilder`, `OssuaryBuilder`, `KilnBuilder`, `BarrensBuilder`, `EncounterBuilder`, `RiskShrineBuilder`,
  `LightingBuilder`, `VfxBuilder`, zuletzt `ZoneSurfaceBuilder.Apply` (Oberflächen). Abnahme: `SandboxBuilder.VerifyDiabloZone` (inkl. `VerifyKiln`, `VerifyBarrens`).
  Builder-Läufe re-serialisieren LFS-Controller (`*Locomotion.controller`, `VendorIdle.controller`):
  diese Seiteneffekte vor dem Commit revertieren.
- Code: `sandbox/Assets/Scripts/` mit Unterordnern `Audio`, `Classes`, `Contracts`, `Difficulty`,
  `Enemies`, `Items`, `Loot`, `Menu`, `Net`, `Party`, `Progression`, `Save`, `Town`, `Vfx`, `Zones`.
- Assets: KayKit (Dungeon 211 Modelle, Adventurers 6, Skeletons 4, Rig_Medium-Animationen) plus
  Blender-Pipeline (`docs/world-art`, `docs/menu-art`). Keine Fremd-IP, kein Asset-Store-Kauf ohne Freigabe.
- Tests: `sandbox/Assets/Tests/{EditMode,PlayMode}`, gemeinsame Helfer in `DiabloTestFixture.cs`.
  Letzter voller Lauf (menu-flow-polish): EditMode 378/378, PlayMode 268/268.

## Systeme (Ist-Stand)

### Steuerung und Nahkampf

- Click-to-Move mit isometrischer Kamera (`DiabloClickController`), WASD vorhanden.
- LMB: ortsfester Kegelschlag, alle Gegner im 120°-Kegel bis 2,2 m (`DiabloMelee.TryConeAttack`),
  keine Positionsänderung, kein Chase. Jeder Schlag und Stoß malt seinen echten Kegel auf den Boden
  (`Vfx/MeleeConeVfx`, gold = Treffer, blass = Fehlschlag, im Schlagtakt), jeder getroffene Gegner bekommt
  einen Aufprallring (`DiabloJuice.OnEnemyHit`, auch für Skill-Treffer).
- RMB: Stoß 3,2 m im 25°-Kegel, Cooldown 1,2 s (`TryThrust`), reicht weiter als `EnemyAttack.Range` 2,0 m.
- Shift halten: Block (`DiabloBlock`), frontal 120°, 50 % Schadensreduktion, 40 % Lauftempo, LMB/RMB
  gesperrt. Frontal geblockte feindliche Projektile kehren um und schädigen Gegner.
  `PlayerHealth.TakeDamage(amount, fromPos)` ist richtungsabhängig; Bodenfeuer und Zonen-Burns wirken voll.
- Treffer-Feel: Animation, Impact-Sound, Hit-Flash an KayKit-Kind-Renderern, Knockback 0,6/0,2 m,
  Solo-Hit-Stop (stellt `Time.timeScale` immer wieder her, aus bei Netcode), Zielbalken
  `DiabloTargetBar` oben mit 3-s-Fade, keine Schadenszahlen.

### Klassen und Skills

`ClassCatalog` mit drei Klassen. Unbekannte `classId` fällt mit Warnung auf Fenwalker zurück.
`DiabloSkills.Skill`-Werte sind ein Netz-Int-Vertrag: nur anhängen.

| Klasse | Rolle | Rig | Skills (Tasten) |
|---|---|---|---|
| Fenwalker | Tank | Knight | Fireball Q, Arrow E, Cleave R, Seismic Slam Space |
| Lanternbearer | Heiler/Support | Mage (Re-Skin auf Knight-Rig, Laterne) | Lantern Bolt Q, Wisps E, Searing Halo R, Veil Step Space, Mending Light F, Renewing Circle G |
| Rogue | DPS | Rogue | Fireball Q, Arrow E, Quick Shot R, Blade Flurry Space |

- Skill-Varianten (`SkillVariantCatalog`): Cleave rundum oder Frontalkegel 90° mit 1,5× Reichweite.
  Searing Halo mitlaufend oder ortsfest mit doppelter Dauer. Wahl solo im Lantern Hold, im Save.
  Koop nutzt die Basisvariante.
- Spezial-Skills skalieren ×1,5 über die Klassen-Faktoren in `ClassDefinition`.
- Klassenwahl `ClassSelectUI` beim Start („Continue saved hero“ bei Save), übersprungen für
  Autoplay, Shotcheck, Koop-Peers und `-class`.
- Charaktererstellung (character-creation): `HeroProfile` in `RunProfile.Hero` (Save v14,
  Migration `V13ToV14` mit Default-Look + Zufallsname aus dem Run-Seed). `ClassAppearance.Apply(cls, profile)`
  setzt Gender-/Build-Proportionen (chest/spine, shoulder/upperarm, Faktoren 0,85–1,25, kein
  Gameplay-Effekt) und Haar-Aufsätze am `head`-Knochen (6 Frisuren, 10 Farben; Helm verdeckt das Haar).
  Koop: `DiabloNetworkPlayer.appearanceCode` (Owner) + serverbereinigter `heroName`; `ShownName`
  zeigt den Heldennamen, der Accountname (`PlayerAccount`) bleibt getrennt.

### Gegner, Begegnungen und Bosse

- Husks mit Mecanim (`HuskLocomotion.controller`, `HuskAnimator`), Steering um Hindernisse,
  Leichen `DiabloCorpse` (versinkt, Deckel 16, auch auf Clients).
- `EnemyAttack`: 2,0 m, 0,4 s Wind-up, 1,5 s, Threat-basierte Zielwahl (`Enemies/ThreatTable`),
  Dornen-Affix wirkt hier. Tank-Slam betäubt und erzeugt hohen Threat.
- Ossuary: 8 Bone Wardens (`RangedCaster`, 6–8 m Abstand, Telegraph, Deckungscheck, Aggro 14 m,
  Leash 10 m), 2 Elites, **The Hollow Abbot** (150 HP, Phase 2 ab 50 %: 3 Husks + Projektilfächer).
- Kiln: 18 Cinder Thralls (Todesexplosion 2 m), 6 Ash Lancer (Sturm 14 m/s mit Telegraph-Linie),
  2 Kiln Elites, 2 Kiln Wardens, **The Kilnmother** (320 HP, Feuerwellen mit 60°-Lücke, Phase 2
  schnellere Wellen + brennender Boden + 4 Thralls). Auf Kindled in Phase 2 stattdessen
  `KilnSectorPattern` (4×90°-Sektoren, max. 2 aktiv, 1,5 s Warnung, 2 s Brand).
- Glass Barrens (`BarrensLayout`, Ankunfts-Plaza wie im Kiln, auch für Caster): 8 **Shardbacks**
  (Nahkampf, `ShardGuard`: frontaler 110°-Bogen schluckt 60 % gerundet, mind. 1 Schaden, Flanke/Rücken
  voll, betäubt ohne Deckung, `EnemyAttack.turnRate` 2,5), 6 **Dune Augurs** (`RangedCaster` + Payload
  `DuneAugur`: `ShardField` unter dem Helden, 1 s Warnring, 3 s Splitter, unblockbar), 2 **Glass Elites**
  (Shardback-Elite, 150°-Bogen, 30 HP). **The Glasswright** (420 HP, Aggro 11 m): Phase 1 Splitterlanzen-
  Fächer (3 × 14 m, 0,9 s Linien-Telegraph, blockbar), Phase 2 ab 50 % drei Glass Pillars mit `GlassWard`
  (gerichtete Treffer halbiert), schnellere Lanzen, `ShardField` unter dem Ziel; Kindled stattdessen
  Shatter Nova (trifft alle außerhalb 4 m). Eigener Musik-Track `BossGlass`, Overlay „GLASS BARRENS CLEARED“.
- Trefferrichtung: `EnemyHealth.TakeDamage(amount, crit, from)` läuft durch `IDamageFilter`-Komponenten
  (Nahkampf, Skills, Projektile, Koop-`ApplyValidated`); Ticks, Dornen und Brände bleiben ungerichtet.
- `EncounterLayout`: drei Begegnungen mit Deckung, getrennter Aggro und Prioritätszielen, sichere
  Ankunft und Rückzug in allen Layoutvarianten.

### Progression, Beute und Wirtschaft

- Level-Up: 1 Attributpunkt (Strength/Vitality/Focus) + 1 Skillpunkt (Rang 1–5, +20 %/Rang),
  `DiabloCharacterUI` (C).
- Beute: nur Rare und Unique (seit 2026-11-23). `ItemRarity.Magic` bleibt als Enum-Wert (Netz-/Save-Vertrag),
  wird aber nie erzeugt: Magic-Gewicht der Droptabellen ging an Rare (Normal 0/98/2, Elite 0/90/10, Unique-Raten
  unverändert, Magic Find skaliert nur noch Unique), Händler 5 Rare + 1 Unique, Startwaffen und Heiltrank Rare, `ItemDatabase.RetireMagic` hebt
  Altbestände (Save v12, Join-Berichte) auf Rare. `AffixLibrary` mit 14 Affixen in Tier 1/2 (u. a.
  Lebensraub ≤ 25 %, Gold Find, Magic Find, Skill-Rang, Cooldown-Reduktion ≤ 40 %, Feuerschaden,
  Dornen), Rare 3–6 Affixe, Vergleichs-Tooltip.
- Itemlevel aus der Dropquelle (Fen 1, Ossuary 5, Kiln 10, Barrens 15; Elite +1, Boss +2), Roll-Brackets
  pro Itemlevel (Bracket 4 ab 15 mit ×1,75), Uniques an der Bracket-Obergrenze. Zonen-Uniques
  **Shardback Aegis** (Rüstung) und **Glasswright Prism** (Helm) nur aus Bracket-4-Quellen (50 % der
  Rüstungs-/Helm-Uniques dort) und garantiert beim ersten Glasswright-Kill.
  Krit-Wertung (5 % Basis, Cap 75 %, 2× Schaden) und Flächen-Skalierung `r·sqrt(1+inc/100)` für
  Trefferprüfung und VFX gleichermaßen.
- Klassengebundene Waffen, Drops zu 70 % passend zur Klasse des Finders. Build-Uniques
  `lancer-ward` (Cleave unterbricht Lancer-Sturm) und `wick-of-the-halo` (Wisps priorisieren Halo-Ziele),
  dazu „Lantern of the Hold“.
- Gold (`Wallet`, `GoldDrop`), Vendor im Lantern Hold (`VendorStock`, Verkauf 25 %, Heiltrank Taste 1),
  Truhe `StashChest` mit 40 Plätzen, Schmiede `SmithStation` (Verwerten 1/3 Material, Umschmieden
  3 Material + 50 Gold, ein Affix-Slot pro Gegenstand dauerhaft, Uniques ausgenommen).

### Zonen, Akt und Wiederspielbarkeit

- Akt-Kette `ActProgress`: Abbot-Kill öffnet den Kiln, Kilnmother-Kill setzt „Akt I abgeschlossen“
  und öffnet die Glass Barrens (`ActProgress.Barrens = 4`, Tor südlich des Kuppelrings), der
  Glasswright-Kill setzt `ZoneBossDefeated`. Keine Aufträge für Zone 4.
- Weltränder: jede Zone ist ummauert. Der Fen hat die `MoorEdge_*`-Erdwälle, Ossuary, Kiln und Barrens je vier
  `ZoneEdge_<Zone>_<Seite>`-Mauern (1,6 m, `ZoneEdgeBuilder`, Zonen-Texturen). Die begehbaren Rechtecke stehen
  in `Zones/WorldBounds` (SSOT für Builder, Begleiter-Clamp und `WorldEdgeTests`).
- `ZoneGate`/`ZoneTravel`, Wegpunkt-Schreine in allen Zonen inkl. „Glass Barrens“ (`KilnLayout.Shrines`,
  Index 3 bleibt Kiln) (`DiabloWaypointUI`).
- `LayoutSeeder`: je 3 Varianten für Fen-Gruppen, Elites, Props, Kiln- und Barrens-Anker, Seed aus `-seed N`
  oder Run-Seed. Koop-Peers bleiben auf Variante 0.
- Kindled (`DifficultyProfile`): HP/Schaden ×1,8, Affix-Tier 2, Unique ×2, Gold ×1,5, freigeschaltet
  beim ersten Abbot-Kill. Einmaliger Kindled-Finale-Anspruch auf eine klassenkompatible Rare-Waffe.
- Optionale Systeme: Auftragsbrett (Jagd + Bergung, einmal pro Run abholbar), Risiko-Schrein im Fen
  (Heilung oder Elite +25 % HP mit garantiertem Rare).

### Gruppe und Koop

- Solo-KI (`Party/PartyCompanion`, `PartyRoster`) füllt fehlende Rollen auf drei auf, folgt, kämpft,
  heilt. Begleiter laufen in Formation hinter dem Anführer (Slots links/rechts/hinten, Aufholen ×1,6 ab 4 m),
  brechen Kämpfe ab 8 m Abstand zum Anführer ab, bleiben auf dem Zonenboden (`WorldBounds.Clamp`) und reisen
  bei jedem `ZoneTravel.TeleportHero` des Anführers mit (`PartyRoster.PortWithLeader`). Koop-Spieler ersetzen die KI ihrer Rolle 1:1 (max. drei Rollen).
- Dreiergruppe (coop-trio, 2026-11-23): Host + zwei Clients spielen Akt I und die Glass Barrens
  komplett durch (`COOP_CLIENTS=2 bash scripts/run-coop-pair.sh`, Host wartet per `-coop-peers 3`).
  Clients buchen die Boss-Kills des Hosts in ihren eigenen Akt-Stand und Save (Abbot → Kindled + Kiln,
  Kilnmother → Akt I + Barrens, Glasswright). Gruppen-Tore verlangen wie solo 0,6 s Verweilzeit und
  senden nach einem Broadcast 2 s lang nicht erneut. Helden blockieren sich nicht gegenseitig
  (`DiabloNetworkPlayer.IgnoreAllyCollisions`). Mitspieler-HP folgen dem Besitzer
  (`DiabloNetworkPlayer.hpState` → `PlayerHealth.SetMirrored`), Heiler sehen verletzte Mitspieler auf
  jedem Prozess. Beleg `docs/coop-run-log.md`.
- NGO-Host-Modell: `-host [-port N]`, `-join ip[:port]`, `-host-relay`, `-join-code` (UGS Multiplayer
  Services 2.5.0, Relay), `JoinPanel` (F7). Host-autoritative Treffer (`Net/HitValidator`),
  Gegner-HP/Phasen/Summons repliziert, Gruppenreise über Tore (`ZoneTravelRelay`), Gold/Vendor/Loot
  per validiertem ServerRpc (`Net/TradeValidator`), Gegenstandsbesitz host-autoritativ mit
  vollständigem Item-Snapshot. Jeder Client speichert seinen eigenen Charakter, Join meldet Level und
  Gear (`SanitizeJoinReport`).
- Broadcasts laufen über das Player-Objekt: dynamisch gespawnte Relay-Objekte replizieren nicht.
- Dedizierter Server (cinderreach-dedicated-server, 2026-11-23): `-server` startet NGO ohne eigenen Helden
  (`Net/DedicatedServer`, `DedicatedServerHost`): Solo-Held abgestellt, Leader = lebender Spieler mit kleinster
  ClientId (Gruppenreise), Broadcasts über ein beliebiges Spielerobjekt, kein Speichern, keine KI-Begleiter,
  60-fps-Deckel, Exit 0 nach 20 s Leere (Neustart = frischer Run). `-server-relay` veröffentlicht über Relay +
  Lobby (`Net/ServerDirectory`, Lobby „Cinderreach Server“ mit Join-Code, Heartbeat 15 s), Clients treten per
  „Join Server“ (Startbildschirm, F7-Panel) oder `-join-server` ohne Code bei, Fehlschlag spielt solo weiter.
  `-server-name` trennt Testläufe. Betrieb: Pod `workspace/cinderreach-server` im devmesh (dotfiles
  `apps/cinderreach-server/k8s`, Flux `ks-cinderreach-server` in Bachelorprojekt), Image per
  `scripts/publish-server-image.sh`, Ablauf in `docs/runbook.md`. Paketlimit 1200 Byte (`CompactPacketDriver`):
  die WireGuard-MTU 1420 des Clusters verwarf volle UTP-Pakete. Server-Build = Server-Subtarget ohne Shader,
  Laufzeit-Material-Code muss `Shader.Find` = null tolerieren. Belege: `docs/coop-run-log.md` (s1-1 bis s4-prod-1).
- Dedizierter Server (cinderreach-dedicated-server, 2026-11-23): `-server` startet NGO ohne eigenen Helden
  (`Net/DedicatedServer`, `DedicatedServerHost`): Solo-Held abgestellt, Leader = lebender Spieler mit kleinster
  ClientId (Gruppenreise), Broadcasts über ein beliebiges Spielerobjekt, kein Speichern, keine KI-Begleiter,
  60-fps-Deckel, Exit 0 nach 20 s Leere (Neustart = frischer Run). `-server-relay` veröffentlicht über Relay +
  Lobby (`Net/ServerDirectory`, Lobby „Cinderreach Server“ mit Join-Code, Heartbeat 15 s), Clients treten per
  „Join Server“ (Startbildschirm, F7-Panel) oder `-join-server` ohne Code bei, Fehlschlag spielt solo weiter.
  `-server-name` trennt Testläufe. Betrieb: Pod `workspace/cinderreach-server` im devmesh (dotfiles
  `apps/cinderreach-server/k8s`, Flux `ks-cinderreach-server` in Bachelorprojekt), Image per
  `scripts/publish-server-image.sh`, Ablauf in `docs/runbook.md`. Paketlimit 1200 Byte (`CompactPacketDriver`):
  die WireGuard-MTU 1420 des Clusters verwarf volle UTP-Pakete. Server-Build = Server-Subtarget ohne Shader,
  Laufzeit-Material-Code muss `Shader.Find` = null tolerieren. Belege: `docs/coop-run-log.md` (s1-1 bis s4-prod-1).

### Konten, Namen und Cloud-Held (cinderreach-accounts, 2026-11-23)

- Identität `Net/PlayerAccount`: UGS-Anmeldung mit Auth-Profil statt Cache-Löschen (normal `main` oder `-profile`,
  Automation/Server Wegwerf-Profil `auto<pid>`). Gäste behalten ihre anonyme Identität. `RelayJoin.EnsureUgsAsync`
  delegiert hierher.
- Namen: UGS Player Names, `PlayerNames.Sanitize` (3–16 Zeichen, Leerzeichen → `_`, `#1234` intern),
  `-player-name` für Läufe. `DiabloNetworkPlayer.playerName` (Server bereinigt, Fallback `Wanderer_<id>`),
  `Net/HeroNameplates` über allen Netz-Helden (eigener gold, tote gedimmt), Beitritts-/Abgangszeilen. Startbildschirm
  „Playing as <Name>“ öffnet `Menu/AccountPanel`.
- Konto: Benutzername + Passwort (Provider im UGS-Projekt aktiv), `SignUpAsync` hängt an einen Gast an (Spieler-ID
  bleibt), `SignInAsync`, `SignOutAsync` (weiter als neuer Gast), Regeln und Fehlertexte in `AccountRules`.
- Cloud-Held `Save/CloudHeroSync`: nur angemeldete Konten (nie Gäste, Automation, Server), Cloud-Save-Schlüssel `hero`
  mit Hülle `{v, savedTicks, owner, save}`, Side-Car `<save>.meta.json`, Upload 2 s nach jedem lokalen Schreiben,
  Abgleich bei jedem Kontowechsel über `CloudHeroResolver` (Upload/Download/Ask/StartFresh), Dialog „WHICH HERO?“,
  ersetzte Helden als `.bak`. Quit-Autosave lädt erst beim nächsten Anmelden hoch.
- Live-Probe `scripts/account-smoke.sh` (Wegwerf-Konto, wird gelöscht), Betrieb im Runbook „Konten, Namen und
  Cloud-Held“. Nicht-Ziele: Server-Validierung des Cloud-Helden, Passwort-Reset, Freunde, Steam, Konto-Löschung im Spiel.

### Präsentation

- Welt-Oberflächen (2026-11-23): 16 kachelbare Textursätze aus `docs/world-art/build_zone_textures.py`
  (numpy, Seeds fest) unter `Art/Zones/Textures` (Albedo mit Smoothness im Alpha, Höhe → Normal-Map beim
  Import, Kiln-Basalt mit Emissions-Adern). `ZoneSurfaceBuilder.Apply` gibt Fen-Boden, Ossuary-Boden/-Pfeiler/
  -Sarkophage/-Deckung, Kiln-Boden/-Kuppeln/-Schlacke, Barrens-Sand/-Dünen/-Spitzen, Schmiede, Auftragsbrett und
  Wegpunkt-Schreine per Namensregel ein Material und Welt-UVs (Box-Projektion, Meter pro Kachel). Die
  Blender-Props bekommen Box-UVs beim Import (`WorldPropUvPostprocessor`) und für Stein, Rinde, Holz, Moos,
  Knochen, Eisen, Tuch die Zonen-Materialien. Spielmechanisch lesbare Flächen (Tore, Glasnarbe, Glaspfeiler,
  Risiko-Schrein, Emissions-Marker) bleiben einfarbig. Kiln-Adern bewusst gedimmt, damit Telegraphen dominieren.
- Zonen-Licht per URP-Volumes (`Settings/Volumes/*`, `ZoneAtmosphere`), Musik pro Zone + Boss
  (`MusicDirector`, Crossfade; 8 eigene 16-s-Kreispuffer-Melodiethemen inkl. `KilnLoop`, Menü-Thema
  ab Startbildschirm, Boss-Thema ab Phase 2 bzw. Glasswright), 35 warme Audio-Cues (`DiabloAudio`,
  3-Varianten-Pools ohne Direktwiederholung für `Swing`/`Hit`/`Footstep`, materialabhängiges
  `PlayHitFor` über 5 Oberflächen, zonenbodenabhängige Schritte, Synth als Fallback,
  `docs/audio/manifest.json`; Vertrag `scripts/check-audio-manifest.py` = Probe `G-DIABLO07`),
  gepoolte VFX (`PresentationVfx`).
- Menüs (`Menu/`): genau ein Startbildschirm `ClassSelectUI` (Startbild, drei Klassenkarten aus
  Rolle/Starter/Skillzahl, Continue mit Spielstand-Info über `SaveService.TryPeek`, Co-op, Settings,
  Quit, 0,45-s-Ausblenden). Das Legacy-`MainMenu` weicht im Spiel (`ClassSelectOwnsStart`).
  Neuer Held fragt nach („BEGIN ANEW?“), wenn ein gespeicherter oder aktueller Held ersetzt würde;
  mitten in der Sitzung (solo) per Szenen-Reload mit frischem `RunProfile`. „New run“ in den Settings
  setzt den Helden nach dem Reload fort (`SaveService.ContinueAfterReload`).
  `PauseMenuUI` (Esc) friert solo die Zeit ein (Hit-Stop-sicher, Koop nie). Offen-Status über
  `ClassSelectUI.AnyOpen`/`PauseMenuUI.AnyOpen`, kein Szenen-Scan.
- UI-Bausteine `DiabloUIFactory` (9-Slice-Chrome aus `DiabloMenuArt`): `SetLayout` setzt auch die
  Rect-Größe, weil die Panels mit `childControlHeight = false` stapeln; Item-Karten per `FitToBox`.
  Sprites aus Blender (`docs/menu-art/build_menu_pack.py`, `build_icons*.py`): Portraits aller drei
  Klassen, Icons für alle 12 Skills, Startbild 1920×1080. Spielernamen über `LootDrop.PrettyName`.
- HUD: Lebenskugel mit Todesschleier („YOU HAVE FALLEN“, rein präsentativ), Bronzeplatte unter
  Skill- und XP-Leiste. Charakterblatt mit Varianten-Umschalter (solo im Lantern Hold),
  Waypoint-Fenster mit gesperrten Schreinen („not attuned“/„sealed“), „Press On“ nach dem Abbot.
  Zielbalken mit Anzeigenamen (`DiabloTargetBar.DisplayName`, Tier-Farbe), Bossbalken mit Phase
  (Boss/ActBoss im Umkreis 24 m), Alt zeigt Loot-Namen am Boden (`DiabloLootLabels`, `LootDrop.Live`).
- Einstellungen `DiabloSettings` (PlayerPrefs). Boot fasst Anzeige nur ohne Automation/`-screen-*`
  an und die Auflösung nur, wenn der Spieler eine gespeichert hat (sonst Gate-Regression ~2,7×).
  Laufzeit-UIs nutzen den HD-Scaler (1920×1080, match 0,5, `DiabloHdTests`).
- Leere Ausrüstungsslots: serialisierte Leerstellen sind leere `ItemInstance`s, `Equipment.IsBlank`/
  `NormalizeEmptySlots` macht sie zu `null`.

### Spielstand

`SaveService` schreibt `cinderreach-save.json` (für Konten zusätzlich Cloud Save, siehe oben) (tmp+rename, Autosave bei Level-Up, Zonenwechsel,
Vendor-Schluss, Quit). `SaveGame.CurrentVersion = 12` (v11: `actTwoBossDefeated`, Migration öffnet die
Barrens für Kilnmother-Bezwinger; v12: Magic-Items werden Rare), Migrationskette v1 → … → v12 in
`SaveMigrations` mit Backup `*.vN.bak`. Neuere Versionen werden nicht geladen und bleiben unberührt.
Neue Felder additiv anhängen, eine Migration schreiben und `SaveMigrationTests` auf `CurrentVersion`
prüfen lassen.
Laden: Solo und Host laden nur über „Continue“ auf dem Startbildschirm; beim Netzwerk-Spawn lädt nur
ein beitretender Client seinen Helden (`DiabloNetworkPlayer.ShouldLoadSaveOnSpawn`). `SaveService.TryPeek`
liest für Menüs ohne Nebenwirkung.

## Qualitätsnetz

- Health Goals in dotfiles `scripts/health_probes/diablo_unity.py` (Goal `diablo-unity-health-goals`):
  `G-DIABLO01` `.meta`-Vollständigkeit, `G-DIABLO02` Szenen-/Asset-/Skript-Vertrag, `G-DIABLO03`
  neueste Testergebnisse, `G-DIABLO04` Runner- und Editor-Dienst, `G-DIABLO05` Windows-Stage-Parität,
  `G-DIABLO06` neuester Autoplay. Neue Prefabs, Skripte oder Pflichtphasen ziehen den Vertrag in
  dotfiles im selben Durchlauf nach.
- Autoplay: `SessionAutoplay` (`-autoplay`, `-autoplay-god`, `-seed`, `-difficulty`, `-class`) schreibt
  `autoplay-result.json` mit den Phasen `town, zone1, gate, zone2, boss, zone3, actboss, zone4, boss3,
  complete` (Koop-Client siehe Koop-Paarlauf).
  `scripts/run-autoplay.sh` (Build → Lauf → `fps-gate.py --min-fps 30` nur als Hinweis, `AUTOPLAY_FPS_STRICT=1` macht es wieder blockierend). Auf Abruf
  per Job `autoplay` in `.github/workflows/unity-tests.yml` (`gh workflow run unity-tests.yml -f autoplay=true`,
  Runner `unity-licensed`, kein Zeitplan seit 2026-11-25) für Normal, Kindled und Lanternbearer, Seed rotiert 0–2. Koop-Paarlauf `scripts/run-coop-pair.sh` (`COOP_RELAY=1` für Relay, `COOP_CLIENTS=2` für die
  Dreiergruppe, `COOP_CLIENT_GOD=0` für sterbliche Clients). Koop-Client-Phasen: `town, zone1, zone2,
  boss, zone3, actboss, zone4, boss3` (je Zone Treffer oder Angriffs-Casts, je Boss die gebuchte Kill-Meldung).
- Installer: `scripts/build-installer.sh` (Inno Setup 6 per winget, `scripts/installer/cinderreach.iss`) packt
  den Windows-Build in `Cinderreach-Setup-<version>-<commit>.exe` (Installation pro Benutzer nach
  `%LOCALAPPDATA%\Programs\Cinderreach`, Startmenü, optional Desktop, Deinstaller; Spielstände bleiben).
  `INSTALLER_DEST` kopiert das Setup in einen Windows-Ordner.
- Windows: `scripts/stage-windows.sh` nach `Games\cinderreach`, Sichtprüfung `scripts/shotcheck-windows.sh`
  (`--autoplay`). Shotcheck/Autoplay schreiben `automation-save.json`, nie den Spieler-Save. Für
  WIP-Builds `STAGE_DEST=…/Games/cinderreach-wip`, `SHOTCHECK_GAME` gleich. 60 FPS @1080p belegt (schlechtestes Fenster 131,5 fps über die volle Route bis zum Glasswright, 2026-11-22). Performance gilt nur auf Windows mit GPU (`shotcheck-windows.sh --autoplay`, 60 FPS @1080p); das Linux-FPS-Gate (WSLg/Mesa d3d12) ist seit 2026-11-22 nur ein Hinweis.
- CI: `unity-tests.yml` (inkl. Autoplay auf Abruf), `unity-scene-captures.yml` (GPU-Runner).
- Bekannte Grenze: Solo-Autoplay ohne `-seed` mit einem Save-Seed außerhalb 0–2 kann in einer
  seltenen Fen-Variante hängen. Der Nightly deckt nur Seeds 0–2 ab.

## Arbeitsweise für neue Goals

1. Lage prüfen: `git -C ~/unity-sandbox fetch && git -C ~/unity-sandbox status --short`. Fremde
   Worktrees mit `git -C ~/unity-sandbox worktree list` und `python3 ~/scripts/agent-workspace.py lock list`
   prüfen. Kollisionen über disjunkte `target_files` je Stufe vermeiden.
2. Goal anlegen (`GOAL.md` mit Objective, Erfolgskriterien, Nicht-Zielen und `health_goals`,
   `PROGRESS.md` mit genau einer `Prozent:`-Zeile), Plan unter `.agents/plans/<slug>/tasks.md` mit Stufen.
3. Pro Stufe ein Worktree: `python3 ~/scripts/agent-workspace.py create --repo unity-sandbox feature/<slug>-sN`,
   im Worktree-Root `lock claim task <slug>-sN`.
4. Verify je Stufe vor dem Commit, im Worktree-Root:
   - `bash scripts/run-unity-tests.sh` (Exit 3 = anderer Lauf hält den Unity-Lock, warten).
     Beim Iterieren `PLATFORMS=EditMode TEST_FILTER=<Klasse> bash scripts/run-unity-tests.sh`.
   - `SandboxBuilder.VerifyDiabloZone` und `KayKitAssets.Verify` (`repo.yaml:verify`).
   - `python3 scripts/check-meta.py`.
   - Bei Gameplay-Änderungen `bash scripts/run-autoplay.sh <out> -seed 0` (Exit 0, alle Phasen); Performance-Nachweis über den Windows-Shotcheck.
   - Bei dotfiles-Anteilen eigener dotfiles-Worktree, `python3 scripts/agent-workspace.py check`,
     `python3 -m pytest scripts/test_diablo_unity_health.py -q`, danach landen.
5. Doku im selben Commit: `PROGRESS.md` mit Belegen, `docs/dev-journal.md`, bei Mechanik-Entscheiden
   `docs/mechanics-questions.md` und `docs/decision-log.md`.
6. Commit (Conventional Commits), dann `python3 ~/scripts/agent-workspace.py land`.
7. Bei 100 %: Stand hier einarbeiten, Goal und Plan entfernen.

Bekannte Fallen: `Camera.main` und `GameObject.Find` pro Frame kosten messbar FPS (cachen, Klick-Gate
zuerst). `Diablo.IsSolo` zählt den Server nicht mit. Library nie bei laufendem
`unity-sandbox-editor.service` kopieren. Synth-Audio und Ambience sind in Testläufen stumm geschaltet.

## Belege und Doku

- Design: `docs/one-page-gdd.md`, `docs/mechanics-questions.md` (Q1–Q10), `docs/decision-log.md`,
  `docs/dev-journal.md`, `docs/playtest-protocol.md`, `docs/fair-fights-playtest.md`.
- Laufprotokolle: `session-loop-run-log.md`, `replayability-run-log.md`, `second-class-run-log.md`,
  `act-one-run-log.md`, `act-two-run-log.md`, `coop-run-log.md`, `presentation-run-log.md`,
  `story-run-log.md` (alle unter `docs/`). Story-SSOT: `docs/story-bible.md`.
- Betrieb: `docs/runbook.md`, `docs/ci-runner.md`, `docs/mcp.md`, `docs/kaykit-assets.md`.

## Konsolidierte Goals

Alle auf 100 % abgeschlossen, Stand oben eingearbeitet.

| Goal | Kern | Abschluss |
|---|---|---|
| `unity-diablo-prototype` (dotfiles) | Click-to-Move, Melee, 3 Gegner, Orb-Loot | `4f00920` |
| `unity-diablo-d2-combat-feel` | Treffer-Feel, Gegenangriff, Zielbalken | `ec7d503` |
| `unity-diablo-feel-polish` | Husk-Animation, Leichen, Steering, Hit-Stop | 2026-11-17 |
| `arpg-session-loop` | Identität, Gold/Save, Vendor, Punkte, Ossuary + Abbot, Autoplay | 2026-11-18 |
| `cinderreach-loop-guard` | Nightly-Autoplay, `G-DIABLO06`, FPS-Gate | `4149cee` |
| `cinderreach-replayability` | Kindled, Layout-Seeds, 14 Affixe, Truhe | `604b879` |
| `cinderreach-second-class` | Lanternbearer, Klassenwahl, Klassen-Items | `3fc2fbc` |
| `cinderreach-act-one` | Cinder Kiln, Kilnmother, Akt-Kette, Wegpunkte | 2026-11-18 |
| `cinderreach-zone-contracts` | Jagd- und Bergungsaufträge | `9ad3b4c` |
| `cinderreach-smithing` | Verwerten und Umschmieden | `d143229` |
| `cinderreach-skill-variants` | Cleave- und Halo-Varianten | `218a3c1` |
| `cinderreach-encounter-roles` | Deckung, getrennte Aggro, Prioritätsziele | `d91035a` |
| `cinderreach-risk-shrines` | Heilung oder Elite-Kampf am Schrein | `f311561` |
| `cinderreach-kindled-finale` | Sektor-Phase und Kindled-Belohnung | `0bdfa7e` |
| `cinderreach-build-uniques` | `lancer-ward`, `wick-of-the-halo` | `59e9ad2` |
| `cinderreach-melee-rework` (dotfiles) | Kegelschlag, Stoß, Block mit Reflexion | `ffbcc0a` |
| `cinderreach-coop-session` | Koop-Parität, Direkt- und Relay-Join | `8f47c17` |
| `cinderreach-presentation` | Licht, Audio, VFX, Menü, 60 FPS | `6325e7d` |
| `cinderreach-menu-flow-polish` | Ein Startbildschirm, Layout-Fix, Rogue-Art, Pause/Tod/Flow, Boot-Perf-Fix | `722c8cf` |
| `cinderreach-act-two-opening` | Akt II: Glass Barrens, Shardback/Augur/Elite, The Glasswright, Bracket 4 + 2 Zonen-Uniques, Save v11, Autoplay bis `boss3` | `c89dc40` |
| `cinderreach-dedicated-server` | Server ohne Helden, Relay + Lobby, „Join Server“, devmesh-Pod, Paketlimit 1200 | `ae2222d` |
| `cinderreach-accounts` | Auth-Profile, Spielernamen + Namensschilder, Konto mit Benutzer/Passwort, Cloud-Held | `02ece6d` |
| `cinderreach-sound-audit-fix` | 35 warme SFX mit Varianten-/Material-Routing, 8 melodische 16-s-Zonenthemen, `G-DIABLO07`, Hörabnahme | `ea59d7b` |
