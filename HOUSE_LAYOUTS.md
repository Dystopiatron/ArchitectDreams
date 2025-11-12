# House Layout Variety Guide

The 3D viewer now generates **5 different architectural layouts** based on your lot size!

---

## How It Works

The layout is determined by your **lot size** using a simple formula:
```
Layout Type = Lot Size % 5
```

This means:
- Lot size **2500** → Layout **0** (Traditional Cube)
- Lot size **2501** → Layout **1** (Two-Story)
- Lot size **2502** → Layout **2** (L-Shaped)
- Lot size **2503** → Layout **3** (Split-Level)
- Lot size **2504** → Layout **4** (Angled Modern)

---

## The 5 Layout Types

### Layout 0: Traditional Single-Story
**Example Lot Sizes:** 2500, 2505, 2510, 2515...

**Features:**
- Single rectangular building
- Classic proportions
- Simple peaked or flat roof
- Windows evenly distributed
- Traditional door placement

**Best for:** Starter homes, bungalows, simple designs

---

### Layout 1: Two-Story House
**Example Lot Sizes:** 2501, 2506, 2511, 2516...

**Features:**
- **Two distinct floors**
- Upper floor slightly smaller than lower
- Creates Victorian/Colonial appearance
- Smaller peaked roof on top
- More vertical design

**Best for:** Victorian style, family homes, maximizing space

---

### Layout 2: L-Shaped House
**Example Lot Sizes:** 2502, 2507, 2512, 2517...

**Features:**
- **Main wing + perpendicular side wing**
- Creates an "L" footprint
- Two separate roof sections (if gabled)
- More interesting architecture
- Natural courtyard area

**Best for:** Modern designs, privacy, corner lots

---

### Layout 3: Modern Split-Level
**Example Lot Sizes:** 2503, 2508, 2513, 2518...

**Features:**
- **Two sections at different heights**
- Contemporary staggered design
- Lower and upper levels offset
- Dynamic appearance
- Modern architectural style

**Best for:** Modern/Contemporary styles, hillside lots

---

### Layout 4: Angled/Rotated Design
**Example Lot Sizes:** 2504, 2509, 2514, 2519...

**Features:**
- **Main section rotated 22.5 degrees**
- **Side section rotated -30 degrees**
- Creates dynamic angles
- Ultra-modern appearance
- Breaks away from grid

**Best for:** Avant-garde designs, artistic expression

---

## Testing Different Layouts

Want to see all 5 layouts? Try these lot sizes:

1. **Traditional Cube:** 2500 sq ft
2. **Two-Story:** 2501 sq ft
3. **L-Shaped:** 2502 sq ft
4. **Split-Level:** 2503 sq ft
5. **Angled Modern:** 2504 sq ft

Then try 2505-2509 to see the cycle repeat!

---

## Style Interactions

### Victorian + Layout 1 (Two-Story)
- Perfect match!
- Gabled roof on upper story
- Classic Victorian proportions

### Modern + Layout 4 (Angled)
- Ultra-contemporary
- Flat roofs emphasize angles
- Large windows on angled faces

### Brutalist + Layout 2 (L-Shaped)
- Interesting concrete masses
- Flat roofs on both wings
- Bold geometric statement

---

## Windows Distribution

Each layout type has **intelligent window placement:**

- **Traditional:** 3 windows evenly spaced on front
- **Two-Story:** Windows on both floor levels
- **L-Shaped:** Windows on both wing sections
- **Split-Level:** Windows on both offset levels
- **Angled:** Windows follow the rotated sections

Windows automatically scale based on:
- Wing/section width
- Style (large/medium/small from stylePrompt)

---

## Roof Adaptations

### Gabled Roofs (Victorian)
- **Traditional:** Single peaked roof
- **Two-Story:** Smaller roof on upper floor
- **L-Shaped:** Two separate peaked roofs (one per wing)
- **Other layouts:** Adapted peak size and position

### Flat Roofs (Modern, Brutalist)
- **Traditional:** Single flat roof with overhang
- **L-Shaped:** Two separate flat roof sections
- **Other layouts:** Flat roofs sized to each section

---

## Future Enhancements

Coming soon:
- Mouse controls to rotate manually
- More layout variations (U-shaped, Courtyard, etc.)
- Interior room divisions
- Garage attachments
- Deck/patio additions
- Pool areas for larger lots

---

## Tips for Architects

**Want a specific layout?**
1. Calculate: Desired layout number (0-4)
2. Choose lot size ending in that number
3. Examples:
   - Need L-shaped? Use 2502, 3002, 4502
   - Need two-story? Use 2501, 3001, 4001

**Want consistency?**
- Use lot sizes that are multiples of 5 (2500, 2505, 2510)
- All will generate Traditional Cube layout

**Want variety?**
- Try 2500-2504 to see all types
- Download OBJ files of each
- Import to Blender/AutoCAD for comparison

---

## Technical Details

**Layout Seed Calculation:**
```javascript
const layoutSeed = houseParams.lotSize % 5; // Returns 0, 1, 2, 3, or 4
```

**Section Creation:**
- Each layout uses modular `createHouseSection()` function
- Sections can be positioned, rotated, scaled independently
- Windows automatically added to each section
- All sections grouped together for unified rotation

**Roof Logic:**
- Checks both `roofType` (gabled/flat) AND `layoutSeed`
- Adaptive positioning based on section heights
- Multiple roofs for multi-wing designs

---

**Experiment with different lot sizes to discover all the architectural possibilities!** 🏗️✨
