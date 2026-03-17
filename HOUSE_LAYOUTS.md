# House Layouts

The app generates 5 different building layouts. The layout is selected by `buildingShapeOverride` in the API request, or by the frontend layout picker. When set to "auto", it's determined by `lotSize % 5`.

---

## Layout Types

| # | Shape | Strategy Class | Description |
|---|-------|---------------|-------------|
| 0 | Cube (default) | `CubeLayoutStrategy` | Single rectangular section |
| 1 | Two-Story | `TwoStoryLayoutStrategy` | Stacked footprints, upper floor 85% of lower |
| 2 | L-Shape | `LShapeLayoutStrategy` | Main wing + perpendicular side wing |
| 3 | Split-Level | `SplitLevelLayoutStrategy` | Two sections at different elevations |
| 4 | Angled | `AngledLayoutStrategy` | Rotated sections (22.5 and -30 degrees) |

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

## Window and Roof Behavior

- Windows are distributed across all exterior walls of all sections
- Multi-section layouts get one roof per section
- L-Shape rooms use `GenerateLShapeRoomLayout()` to avoid placing geometry in the void quadrant (front-right area of the L)
