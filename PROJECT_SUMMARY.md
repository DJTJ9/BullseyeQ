# DartTrainingsApp – Projektzusammenfassung

Unity-Projekt (`C:/Unity/Aktuelle Projekte/DartTrainingsApp/`), Rider 2026.1, UI Toolkit (kein uGUI/IMGUI).

---

## Vision & Roadmap

| Phase | Status | Inhalt |
|-------|--------|--------|
| 1 – Tracking | ✅ Fertig | Trainingswürfe erfassen und analysieren |
| 2 – KI-Gegner | 🔜 Geplant | Adaptiver AI-Gegner (501, Double Out, an Spielerstärke angepasst) |
| 3 – Trainingsspiele & Pläne | 🔜 Geplant | Strukturierte Trainingsprogramme |

---

## Architektur

```
GameManager (Execution Order -100)
  └── DataManager (Singleton, Runtime State)
        ├── PlayerProfile (JSON-persistiert)
        │     └── TrainingSession[] (polymorph: Scoring / 501 / Checkout)
        └── CurrentSession (aktive Session)

UI (UIDocument, DartInput.uxml)
  ├── DartInputController    → SessionStatsPresenter, OverviewPresenter, StatsPresenter
  ├── FiveOhOneController    → FiveOhOneStatsPresenter
  ├── CheckOutController     → CheckOutStatsPresenter (je Modus)
  └── SettingsController
```

**Muster:**
- **Presenter Pattern** – Controller kennt nur Presenter, kein direktes UI-Binding in der Logik
- **Immutable Session Data** – Stats nur in `RecalculateStats()`, private Setter mit `[JsonProperty]`
- **Polymorphe Persistenz** – Newtonsoft JSON mit `TypeNameHandling.Objects`
- **Rules Engine** – `DartRules.cs` zentralisiert Double-Out-Logik
- **Custom VisualElements** – Heatmap + Linechart via `Painter2D`

---

## Eingabeformat (Domänenkonvention)

| Eingabe | Bedeutung |
|---------|-----------|
| `20` | Single 20 |
| `20+` | Triple 20 |
| `20-` | Double 20 |
| `25` | Bull (25 Punkte) |
| `25-` | Bullseye (50 Punkte) |

Gültige Felder: 1–20 und 25. Kein Triple-Bull.

---

## Trainingsmodi

### Scoring
Freies Scoring-Training, 3 Pfeile pro Runde.

**Stats:** Session-Avg, Rolling-Avg (letzte 5 Sessions), Triple-Quote (T18+T19+T20), Wasted-Dart-Rate (< 18 Punkte), Heatmap.

### 501 (Double-Out)
Startet bei 501, Finish nur auf Double.

**Features:** Bust-Erkennung, Auto-Finish bei Checkout, Finish-Routen-Vorschläge (bis 6).
**Stats:** 3-Dart-Average, Checkout-Rate, Darts-to-Finish, letzte 5 Legs.

### Checkout-Training (3 Modi)

| Modus | Beschreibung |
|-------|-------------|
| Target Double | Gezielt auf bestimmtes Double trainieren |
| Checkout Challenge | Vorgegebene Checkout-Scores treffen |
| Five Checkouts | 5 Checkouts auf steigendem Schwierigkeitsgrad |

---

## Datenmodell

```
PlayerProfile
  └── TrainingSession[]  (abstract, SessionType: Scoring / FiveOhOne / CheckOut)
        ├── ScoringSession
        │     └── ScoringRound[] → DartArrow[3]
        ├── FiveOhOneSession
        │     └── FiveOhOneVisit[] → DartArrow[1-3]
        └── CheckOutSession
              └── CheckOutRound[] → DartArrow[]
```

**Stats werden nicht gespeichert**, sondern bei jedem Laden über `RecalculateStats()` neu berechnet.

---

## Statistik-Details

- **Triple-Quote**: T18 + T19 + T20 zusammen (T20 oft durch vorherigen Pfeil verdeckt → dynamisches Ziel)
- **Wasted Dart**: Wurf < 18 Punkte
- **Rolling Average**: Nur abgeschlossene Sessions (`endTime != null`), Standard letzte 5

---

## UI-Struktur

```
Sidebar
  ├── Overview       – Lifetime-Stats, Rolling-Avg, Trend, Session-Liste, Heatmap
  ├── Training Game  – Placeholder ("Coming soon")
  ├── Training Sessions
  │     ├── Scoring  – Input, Rundenliste, Stats, Heatmap
  │     ├── 501      – Input, Score-Anzeige, Finish-Routen, Stats, Heatmap
  │     └── Checkout – 3 Modus-Subtabs, je Input/History/Stats/Heatmap
  ├── Stats & Analytics
  │     ├── Scoring-Tab  – Linechart, Score-Verteilung, Lifetime-Stats, Heatmap
  │     ├── 501-Tab      – dto.
  │     └── Doubles-Tab  – dto.
  └── Settings       – Stats-Reset mit Bestätigung
```

---

## Schlüssel-Dateien

| Datei | Zweck |
|-------|-------|
| `Scripts/GameManager.cs` | Entry Point, Execution Order -100 |
| `Scripts/DataManager.cs` | Singleton, Runtime State, I/O |
| `Scripts/Data/PlayerProfile.cs` | Persistiertes Spielerprofil |
| `Scripts/Data/DartArrow.cs` | Einzelwurf inkl. `TryParse()` |
| `Scripts/Data/DartRules.cs` | Double-Out-Regelwerk |
| `Scripts/Data/CheckoutChart.cs` | Checkout-Routen-Engine (gecacht) |
| `Scripts/ProfileStorage.cs` | JSON Load/Save |
| `Scripts/DartInputController.cs` | Scoring-Tab Controller |
| `Scripts/FiveOhOneController.cs` | 501-Tab Controller |
| `Scripts/CheckOutController.cs` | Checkout-Tab Controller |
| `Scripts/DartboardHeatmapElement.cs` | Custom VisualElement, Painter2D |
| `Scripts/LineChartElement.cs` | Custom VisualElement, Linechart |
| `UI/DartInput.uxml` | Haupt-UI-Layout |
| `UI/TrainingsSessionStyle.uss` | Stylesheet (Dark Blue + Teal) |

---

## Tests

`Assets/_Project/Tests/EditMode/`:
- `DartArrowTests.cs` – Parsing & Validation
- `ScoringSessionTests.cs` – Rundenverwaltung, Stats
- `FiveOhOneSessionTests.cs` – Visits, Busts, Checkouts
- `PlayerProfileTests.cs` – Rolling Average, Aggregation
- `CheckoutChartTests.cs` – Routen-Generierung
- `JsonRoundtripTests.cs` – Serialisierungs-Roundtrip

---

## Scene-Setup (manuell in Unity)

1. GameObject `GameManager` mit `GameManager.cs`
2. GameObject `DartInput` mit `UIDocument` (Source: `DartInput.uxml`) + `DartInputController.cs` + `FiveOhOneController.cs` + `CheckOutController.cs`
3. Script Execution Order: `GameManager` = **−100**

---

## Offene Punkte

- [ ] Session-History-Screen (alle Sessions einsehbar)
- [ ] Lifetime-Statistiken-Screen
- [ ] KI-Gegner (`DartAI.cs` ist noch leeres Skeleton)
- [ ] Trainingsspiele und Trainingspläne (Phase 3)