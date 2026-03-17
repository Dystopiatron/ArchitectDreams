# House Layouts

The app generates 5 different building layouts. The layout is selected by `buildingShapeOverride` in the API request, or by the frontend layout picker. When set to "auto", it's determined by `lotSize % 5`.

---

## Layout Types

| # | Shape | Strategy Class | Description |
|---|-------|---------------|-------------|
| 0 | Cube (default) | `CubeLayoutStrategy` | Single rectangular section |
| 1 | Two-Story | `TwoStoryLayoutStrategy` | Stacked footprints, upper floor 85% of lower |
| 2 | L-Shape | `LShapeLayoutStrategy` | Main wing + perpendicular side wing |
| 3 | Split-Level | `SplitLevelLayoutStrategy` | Main 2-story (right 50%) + 1-story wing (left 50%), max 2 stories |
| 4 | Angled | `AngledLayoutStrategy` | Main tower (70% at center) + first-floor wing (50% offset to front-right) |

Each strategy implements `ILayoutStrategy.CalculateLayout(width, depth, ceilingHeight, stories)` and returns a `LayoutData` with sections and roof sections.

---

## Auto-Selection Formula

When layout is "auto":
```
layoutSeed = lotSize % 5
```

So lot size 2500 = Cube, 2501 = Two-Story, 2502 = L-Shape, 2503 = Split-Level, 2504 = Angled.

---

## Style Interactions

Each layout works with all 3 styles. Some notable combos:

- **Victorian + Two-Story** — gabled roof on upper story, classic proportions
- **Modern + Angled** — flat roofs emphasize the angular geometry
- **Brutalist + L-Shape** — flat roofs on both wings, bold geometric mass

Roof type comes from the style (flat or gabled), not the layout. Multi-section layouts (L-Shape, Split-Level, Angled) get separate roof sections per wing.

---

## Layout-Specific Room Generation

Some layouts require custom room placement to ensure windows and interior walls appear correctly:

| Layout | Function | Rationale |
|--------|----------|----------|
| L-Shape | `GenerateLShapeRoomLayout()` | Avoids void in front-right quadrant |
| Angled | `GenerateAngledRoomLayout()` | Rooms in tower + synthetic wing room for exterior windows |
| Split-Level | `GenerateSplitLevelRoomLayout()` | Floor 2 rooms only in main section (right 50%) |

---

## Window and Roof Behavior

- Windows are distributed across all exterior walls of all sections
- Multi-section layouts get one roof per section
- L-Shape rooms use `GenerateLShapeRoomLayout()` to avoid placing geometry in the void quadrant (front-right area of the L)
- Angled layouts add synthetic "Wing Room" for sections extending beyond footprint
- Split-level floor 2 is restricted to main section to prevent floating interior walls
