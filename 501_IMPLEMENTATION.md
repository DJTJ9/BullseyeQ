# 501-Trainingssession – Implementierungs- & Debug-Handoff

> Stand: 2026-06-03. Dieses Dokument fasst die komplette 501-Implementierung zusammen,
> damit mit frischem Kontext debuggt werden kann. Es nennt Dateien, Regeln, exakte
> erwartete Stat-Werte, Designentscheidungen, manuelle Unity-Schritte und Risiken.

---

## 0. Bugfixes (2026-06-03, zweite Runde)

Vier vom Nutzer gemeldete Bugs behoben (noch nicht kompiliert/getestet, Editor-Lock):

1. **Fokus auf 501-Tab**: `FiveOhOneController.OnEnable` abonniert jetzt
   `TabView("session-tabs").activeTabChanged`; beim Wechsel auf den 501-Tab wird Feld 0 fokussiert
   (erkannt daran, dass der aktive Tab `fo-dart-field-0` enthält). Verifiziert: API existiert in 6000.4.
2. **Erste Session wurde nicht gespeichert/beendet** (Orphan-Bug): Die 501-Session wurde im Controller-`OnEnable`
   erzeugt, das vor `GameManager.Awake`→`LoadProfile` laufen kann → Session landete im alten, danach ersetzten
   Profil. **Fix**: Session-Erzeugung nach `GameManager.Awake` (nach `LoadProfile`) verschoben; Controller liest
   den Zustand erst in `Start()` (läuft garantiert nach allen Awakes). `EndAndSaveFiveOhOne`/`FinishCurrentFiveOhOne`
   finishen leere Legs nicht mehr (sonst erschiene die ungespielte Start-Session in der „letzte 5“-Liste).
3. **Re-Submit zählte doppelt**: `OnFieldSubmit` liest den Visit jetzt **feldbasiert** (Feldwerte 0..index neu parsen
   + sequenziell auswerten) statt Darts anzuhängen. Korrigiert man ein Feld vor Abschluss des Wurfs, zählt nur der
   zuletzt bestätigte Wert. `_pendingArrows` entfernt; `LiveRemaining`/`OnRemoveLast` ebenfalls feldbasiert.
4. **Mehrere Finish-Routen**: `CheckoutChart.GetCheckouts(remaining, max)` neu (alle Routen, beste zuerst:
   wenigste Darts → Finisher-Präferenz → Setup-Kosten). `GetCheckout` liefert weiterhin nur die beste (= erste).
   Links werden bis zu `MaxFinishRoutes = 6` Routen angezeigt (beste hervorgehoben).

Nachträge (gleiche Runde):
5. **Scoring-Tab-Fokus**: `DartInputController.OnEnable` abonniert ebenfalls `activeTabChanged` und
   fokussiert Feld 0 beim Wechsel auf den Scoring-Tab (erkannt an `dart-field-0`).
6. **Save & End beendet die App**: Beide `OnSaveEnd` rufen nach dem Speichern `AppControl.Quit()`
   (neu: `Scripts/AppControl.cs`) — im Build `Application.Quit()`, im Editor `EditorApplication.isPlaying = false`.

> Hinweis: §7/§11 unten beschreiben teils den Stand VOR diesen Fixes — bei Abweichung gilt dieser Abschnitt.

## 1. Status

- **Code vollständig geschrieben** (Daten, Logik, UI, Controller, Tests).
- **NICHT verifiziert**: Es fand keine Kompilierung und kein Testlauf statt.
  Unity-Batchmode bricht mit `return code 1` ab, solange das Projekt im Editor geöffnet ist
  (Multi-Instance-Lock). Headless-Tests gehen nur bei geschlossenem Editor.
- **Erster Schritt beim Debuggen**: kompilieren lassen (Editor fokussieren → Auto-Recompile,
  Console auf `error CS…` prüfen) **oder** Editor schließen und EditMode-Tests headless laufen lassen
  (Befehl siehe §8).

---

## 2. Vision-Einordnung

Phase 1 (Scoring-Tracking) war fertig. 501 ist die erste echte Trainings-Session-Variante:
Simulation eines 501-Single-Player-Legs nach Standardregeln (Double-Out). **Eine Session = ein Leg.**

---

## 3. Domänen-Grundlagen (bestehend, unverändert)

- **Wurf-Eingabeformat** (`DartArrow.TryParse`): Zahl = Single, `+` = Triple, `-` = Double,
  `25` = Outer Bull (Single, 25), `25-` = Bullseye (Double Bull, 50). Kein Triple-Bull. Gültig: 1–20 und 25.
- **FieldKey** (Heatmap): `T20`, `D5`, `S7`, `25` (Outer Bull), `Bull` (Bullseye).
- `DartArrow.multiplier`: 1 = Single, 2 = Double, 3 = Triple. **Bullseye `25-` hat multiplier 2** (gilt als Double).

---

## 4. Dateien

### Neu
| Datei | Zweck |
|---|---|
| `Assets/_Project/Scripts/Data/DartStats.cs` | Geteilte Helfer: `IsTriple`, `IsLowScore` (<18), `Tally` (Heatmap). |
| `Assets/_Project/Scripts/Data/DartRules.cs` | `enum DartResult {Continue,Bust,Checkout}`; `StartScore=501`; `IsOneDartFinish`; `IsDoubleOut`; `Evaluate(remainingBefore, dart, out remainingAfter)`. |
| `Assets/_Project/Scripts/Data/CheckoutChart.cs` | Checkout-Solver. `GetCheckout(remaining)` → Routen-String oder `null`. `IsCheckoutable`. |
| `Assets/_Project/Scripts/Data/FiveOhOneVisit.cs` | Ein Visit (1–3 Darts) mit `busted`/`checkout`/`scoredPoints`/`dartsThrown`. |
| `Assets/_Project/Scripts/Data/FiveOhOneSession.cs` | Leg mit Replay-`RecalculateStats`. Alle 501-Stats. |
| `Assets/_Project/Scripts/FiveOhOneStatsPresenter.cs` | Bindet rechtes Stats-Panel + letzte-5-Liste + Heatmap. |
| `Assets/_Project/Scripts/FiveOhOneController.cs` | MonoBehaviour, treibt 501-Tab, Dart-für-Dart. |
| `Assets/_Project/Tests/EditMode/FiveOhOneSessionTests.cs` | Stat-Logik via echtem Dart-für-Dart-Flow. |
| `Assets/_Project/Tests/EditMode/CheckoutChartTests.cs` | Solver: bekannte Routen, Bogeys, validiert jede Route 2–170. |

### Geändert
| Datei | Änderung |
|---|---|
| `Data/SessionType.cs` | `FiveOhOne` ergänzt. |
| `Data/ScoringSession.cs` | Dart-Loop nutzt jetzt `DartStats` (Verhalten identisch). |
| `Data/PlayerProfile.cs` | `RecalculateStats` zählt 501-Visits als Runden; neu: `RecentFiveOhOneSessions(count=5)`. |
| `Scripts/DataManager.cs` | Neuer `CurrentFiveOhOneSession`-State + Start/AddVisit/RemoveLastVisit/Reset/SaveAndStartNew/EndAndSave/Finish. |
| `Scripts/GameManager.cs` | `OnApplicationQuit` finished auch das 501-Leg. |
| `UI/DartInput.uxml` | 501-Tab-Platzhalter durch volles Layout ersetzt (alle Elemente mit `fo-`-Präfix). |

---

## 5. Spielregeln (in `DartRules.Evaluate`)

Pro Dart, gegen `remainingBefore`:
- `after == 0 && IsDoubleOut(dart)` → **Checkout** (Leg gewonnen).
- `after < 0` **oder** `after == 1` **oder** (`after == 0 && kein Double`) → **Bust**.
- sonst → **Continue**.

**Bust-Folgen** (im Session-Replay): Visit zählt 0 Punkte, Rest springt auf Visit-Start zurück,
Visit zählt als **3 Darts** (auch wenn weniger geworfen).

**Auto-Ende**: Bei Checkout wird das Leg sofort finished + gespeichert
(`EndAndSaveFiveOhOne`), Input gesperrt (`_legFinished`). Neues Leg nur per „New Session“-Knopf.

---

## 6. Stat-Definitionen (exakt, in `FiveOhOneSession.RecalculateStats`)

Replay aller Visits von 501; `remaining` läuft mit (Bust = Rücksprung).

- `remaining`: aktueller Rest (0 nach gewonnenem Leg).
- `totalDartsThrown`: Σ `visit.dartsThrown` (Bust = 3). Denominator für 3-Dart-Average.
- `threeDartAverage` = `scored / totalDartsThrown * 3` (scored = Σ scoredPoints; Bust = 0).
- `tripleHitRate` = Triples / **tatsächlich geworfene Darts** (nicht Bust-gepolstert).
- `wastedDartRate` = Wasted / tatsächlich geworfene Darts.
  - **Wasted** = `score < 18` **außer** der Dart hat ein 1-Dart-Finish erst ermöglicht
    (`!IsOneDartFinish(before) && IsOneDartFinish(after)`) **oder** er hat ausgecheckt.
- `dartsToFinishPossible` (`int?`): Anzahl geworfener Darts (Bust = 3 gepolstert) bis erstmals
  `IsOneDartFinish(rest)` gilt. `null` falls nie erreicht. Detection nur in **nicht-gebusteten** Visits.
- `checkoutAttempts`: Darts geworfen, während `IsOneDartFinish(remainingBefore)` (= saß auf einem Match-Double).
- `checkoutHits`: davon ausgecheckt.
- `checkoutRate` (`float?`) = hits/attempts; `null` wenn keine Versuche → Anzeige „n.a.“.
- `wonLeg`: letzter Visit ist Checkout.

`IsOneDartFinish(r)` := `r == 50 || (r gerade && 2 ≤ r ≤ 40)`.

### Ground-Truth (aus den Tests – beim Debuggen als Soll-Werte nutzen)

**9-Darter** `20+ 20+ 20+ | 20+ 20+ 20+ | 20+ 19+ 12-`:
remaining 0, totalDarts 9, avg 167.0, dartsToFinishPossible 8, attempts 1, hits 1, checkoutRate 1.0,
tripleHitRate 8/9, wastedRate 0, wonLeg true.

**Bust** `20+ ×9` (3. Visit bustet bei 21 → −39):
remaining 141, totalDarts 9, avg 120.0, dartsToFinishPossible null, checkoutRate null, wonLeg false,
visits[2].busted true, scoredPoints 0.

**Wasted-Ausnahme** `20+ 20+ 20+ | 20+ 20+ 20+ | 20+ 16 15 | 25-`:
remaining 0, totalDarts 10, wastedRate 1/10 (nur S16 wasted, S15 ermöglicht Finish auf 50),
dartsToFinishPossible 9, checkoutRate 1.0, wonLeg true.

**Checkout-Quote mit Fehlwurf** `… | 20+ 7+ 20 | 20 10-` (sitzt auf 40, S20 verfehlt → 20, D10 checkt):
attempts 2, hits 1, checkoutRate 0.5.

---

## 7. Checkout-Solver (`CheckoutChart`)

Kein Hand-Tabelle, sondern Suche: 1/2/3 Darts, letzter Dart immer Double, summiert exakt auf `remaining`.
Heuristik bevorzugt „Standard“-Routen: hohe Triples als Setup (T20 zuerst), konventionelle Schluss-Doubles
(`D20, D16, D8, …`). Bogey-Zahlen (169,168,166,165,163,162,159) und >170/<2 → `null` (kein Sonderfall nötig).
Ergebnisse pro Score gecached.

**Korrektheitsnetz** (`CheckoutChartTests.EveryReturnedRoute_IsValid`): für jedes 2–170 wird –
falls Route ≠ null – geprüft: ≤3 Darts, Summe == n, endet auf Double. Falls hier ein Test rot wird,
liegt der Bug im Solver, nicht in der Spiel-Logik.

Bekannte Soll-Routen: `40→D20`, `50→Bull`, `100→T20 D20`, `160→T20 T20 D20`,
`170→T20 T20 Bull`, `167→T20 T19 Bull`.

---

## 8. Verifikation / Tests laufen lassen

**Editor geschlossen**, dann (Pfade ggf. anpassen):
```
"C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe" \
  -batchmode -projectPath "C:/Unity/Aktuelle Projekte/DartTrainingsApp" \
  -runTests -testPlatform EditMode \
  -testResults "<repo>/test-results.xml" -logFile "<repo>/unity-test.log"
```
Exit 0 = alle grün. Bei Lock erscheint im Log „terminate with return code 1“ direkt nach dem Projektpfad.
Alternativ im offenen Editor: `Window > General > Test Runner > EditMode > Run All`.

---

## 9. Manuelle Unity-Schritte (NICHT im Code)

1. **`FiveOhOneController` als Component aufs „DartInput“-GameObject** legen
   (dasselbe GameObject mit `UIDocument` + `DartInputController`). Beide Controller teilen dasselbe
   UIDocument-Root; Konflikte vermieden durch `fo-`-Präfix der 501-Element-Namen.
2. Prüfen, dass das `UIDocument` weiterhin `DartInput.uxml` als Source nutzt (unverändert).
3. Script Execution Order: `GameManager = −100` bleibt; läuft vor beiden Controllern (lädt Profil).

---

## 10. UXML-Element-Namen (für Q<>-Lookups)

Input: `fo-dart-field-0/1/2`, `fo-feedback-label`.
Links: `fo-finishes-container` (in `fo-finishes-scroll`).
Mitte: `fo-current-score`, `fo-throws-container`/`fo-throws-scroll`,
`fo-recent-sessions-container`/`fo-recent-sessions-scroll`,
Buttons `fo-btn-remove-last`, `fo-btn-new-session`, `fo-btn-reset-session`, `fo-btn-save-end`.
Rechts: `fo-stat-darts`, `fo-stat-avg`, `fo-stat-triple`, `fo-stat-wasted`,
`fo-stat-darts-to-finish`, `fo-stat-checkout`, `fo-heatmap-container`.

> Wenn ein Element zur Laufzeit `null` ist (NullRef im Controller/Presenter): Name-Tippfehler
> zwischen UXML und C#-`Q<>` ist die wahrscheinlichste Ursache.

---

## 11. Controller-Flow (`FiveOhOneController`)

- `OnEnable`: startet bei Bedarf ein 501-Leg, queryt Elemente, registriert Callbacks
  (NavigationSubmit + KeypadEnter pro Feld), erzeugt Presenter, setzt `_visitStartRemaining`/`_legFinished`.
- `OnFieldSubmit(index)`: parst, `DartRules.Evaluate(before, …)`, dann Checkout/Bust/Continue.
  - `_pendingArrows` = Darts des laufenden Visits; `before = _visitStartRemaining − Σ pending`.
- `CommitVisit(busted, checkout)`: baut `FiveOhOneVisit`, `DataManager.AddVisitToCurrentFiveOhOne`,
  pending leeren, `_visitStartRemaining = Session.remaining`.
- `RefreshAll`: Rest-Label, Wurf-History (voller Rebuild aus `Session.visits`), Finishes links, Stats rechts.
- Buttons: New = Save+neues Leg; Reset = Leg auf 501 ohne Speichern; Save&End = finishen (Hauptmenü TODO);
  Remove Last = erst pending-Dart, sonst letzten committeten Visit (öffnet ggf. gewonnenes Leg wieder).

---

## 12. Risiken / zuerst prüfen, falls etwas klemmt

1. **Kompiliert es?** Neue Sprachfeatures genutzt: `visits[^1]` (Index-from-end), `int?`/`float?`-Properties
   mit `[JsonProperty]`-private-set. Sollte unter Unity 6000.4 (C# 9+) gehen – aber als Erstes checken.
2. **JSON-Polymorphie**: Läuft über `TypeNameHandling.Objects` (kein Converter). 501-Roundtrip-Tests
   decken das ab. Alte `player_profile.json` bleibt kompatibel.
3. **Deserialisierung ruft `FiveOhOneSession()`-Ctor** (mit `RecalculateStats()` auf leer), danach
   überschreibt Newtonsoft Felder + Stat-Properties mit den persistierten Werten. Kein Re-Calc danach –
   persistierte Stats müssen also stimmen (tun sie, da bei jedem AddVisit gespeichert).
4. **Zwei Controller, ein UIDocument**: `Q<>` findet Elemente beider Tabs im selben Baum. Wenn ein
   `fo-`-Name versehentlich doppelt zu einem Scoring-Namen ist → falsches Element. (Aktuell disjunkt.)
5. **Remove Last nach Checkout**: setzt `endTime=null`/`durationSeconds=0`, um das Leg wieder zu öffnen.
   `_startDateTime` (privat, nicht serialisiert) lebt im Speicher weiter → erneutes `Finish()` rechnet korrekt.
   Nach App-Neustart wäre eine wiedereröffnete, dann erneut beendete Session zeitlich ungenau (Edge-Case).

---

## 13. Bewusste Designentscheidungen (vom User bestätigt)

- Alle drei Bust-Regeln aktiv (inkl. Rest = 1 und 0-ohne-Double).
- Bust = 3 Darts / 0 Punkte fürs Scoring; **tatsächlich geworfene Darts zählen weiter** für
  Heatmap/Triple/Wasted (echte Technik-Daten). → `totalDartsThrown` (Avg-Denominator) kann von der
  Rate-Denominator-Zahl (echte Darts) abweichen, wenn ein Bust <3 Darts hatte. Dokumentiert.
- Wasted-Ausnahme (Dart ermöglicht/vollendet Finish, siehe §6).
- Finishes links zeigen **eine** empfohlene Route zum aktuellen Live-Rest (Solver), unabhängig von
  verbleibenden Darts im Visit – Vereinfachung; ggf. später auf „Darts-übrig“ verfeinern.
- 501-Legs fließen in `PlayerProfile.lifetimeAverage`/`totalRoundsThrown` (Visit = eine Runde).
  `RollingAverage` (Scoring-Tab „Ø last 5“) bleibt bewusst Scoring-only.

---

## 14. Offene Punkte / mögliche Verfeinerungen

- Hauptmenü-Navigation (Save&End loggt nur, `TODO` im Controller).
- Finishes evtl. auf „mit N verbleibenden Darts im Visit“ verfeinern.
- Solver-Routen sind valide, aber nicht garantiert chart-identisch in jedem Einzelfall.
- Tab-Wechsel-Verhalten: Scoring- und 501-Session laufen unabhängig parallel (gewollt). Falls später
  ein „aktiver Modus“ nötig wird, hier ansetzen (`DataManager`).
