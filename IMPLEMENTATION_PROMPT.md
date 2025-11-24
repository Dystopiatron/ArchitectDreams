# Implementation Prompt - Architectural Dream Machine Fixes

## Context
This prompt summarizes the 25 critical issues found in the comprehensive code analysis and provides guidance for systematic implementation of fixes.

## Critical Issues Requiring Immediate Attention

### 🔴 CRITICAL PRIORITY (Fix First)

1. **ISSUE #3: Roof Pitch Mathematical Error**
   - **Location:** `HouseViewer3D.js` line 588
   - **Problem:** Roof height calculated as `(width/2) * pitchRatio` instead of `roofRadius * pitchRatio`
   - **Impact:** Roofs are 45% too shallow (6:12 pitch renders as 3.3:12)
   - **Fix:** Change height calculation to match diagonal distance to corner

2. **ISSUE #11: Three.js Memory Leak**
   - **Location:** `HouseViewer3D.js` lines 874-885
   - **Problem:** Geometries and materials never disposed
   - **Impact:** Browser memory grows indefinitely, eventual crash
   - **Fix:** Add proper disposal of all Three.js objects in cleanup

3. **ISSUE #12: CORS Security Vulnerability**
   - **Location:** `Program.cs` lines 13-20
   - **Problem:** `AllowAnyOrigin()` exposes API to any website
   - **Impact:** API abuse, resource theft, DOS attacks
   - **Fix:** Restrict to specific origins, add rate limiting

4. **ISSUE #2: Stories Override Breaks Interior Walls**
   - **Location:** Backend generates rooms for original story count, frontend override leaves upper floors empty
   - **Impact:** 3-story building only has walls on floor 1
   - **Fix:** Backend endpoint to regenerate with story count, or frontend duplicates room pattern

5. **ISSUE #1: Backend/Frontend Coordinate System Mismatch**
   - **Location:** Backend generates rooms for full rectangle, frontend creates varied shapes
   - **Impact:** Walls protrude outside building geometry
   - **Fix:** Make backend layout-aware or move all room generation to frontend

### 🟠 HIGH PRIORITY (Fix Second)

6. **ISSUE #4: Wall Height Arbitrary Magic Numbers**
   - Replace 60%/30% with actual roof geometry calculations

7. **ISSUE #5: OBJ Export Doesn't Match 3D Viewer**
   - Backend exports simple cube, frontend shows detailed architecture

8. **ISSUE #6: Unused Backend Data**
   - room.WindowCount, room.Name, room.HasDoor, shape parameter all unused

9. **ISSUE #13: No Caching Strategy**
   - StyleTemplate queries and room generation recalculated every request

10. **ISSUE #24: Building Bounds Redundancy**
    - Bounds recalculated inside loop for every wall instead of once per floor

### 🟡 MEDIUM PRIORITY (Fix Third)

11. **ISSUE #7: Layout Seed Mapping Fragility**
12. **ISSUE #8: Hardcoded 1.5:1 Aspect Ratio**
13. **ISSUE #9: Magic Numbers Throughout**
14. **ISSUE #10: Monolithic Room Generation**
15. **ISSUE #14: Hardcoded StyleTemplates**
16. **ISSUE #15: Package Version Inconsistency**
17. **ISSUE #16: No Coordinate System Documentation**

### Additional Issues (Fix Last)

18-25. Various code quality, scalability, and documentation improvements

---

## Implementation Strategy

### Phase 1: Critical Fixes (Days 1-3)
**Goal:** Fix mathematical errors, memory leaks, and security vulnerabilities

**Tasks:**
1. Fix roof pitch calculation (Issue #3)
2. Implement Three.js disposal (Issue #11)
3. Restrict CORS and add rate limiting (Issue #12)
4. Add story override warning (Issue #2 - temporary fix)

**Success Criteria:**
- Roofs have correct pitch matching specification
- Memory usage stable after 50+ regenerations
- API only accessible from whitelisted origins
- User warned when overriding stories

### Phase 2: Data Flow & Architectural Issues (Days 4-7)
**Goal:** Fix backend/frontend coordination issues

**Tasks:**
1. Implement backend endpoint for story override (Issue #2)
2. Make backend room generation layout-aware (Issue #1)
3. Implement Three.js OBJExporter (Issue #5)
4. Remove unused backend calculations (Issue #6)

**Success Criteria:**
- Multi-story buildings have walls on all floors
- Interior walls respect building footprint per layout
- OBJ export matches 3D viewer
- API payload reduced by removing unused data

### Phase 3: Performance & Code Quality (Days 8-10)
**Goal:** Improve performance and maintainability

**Tasks:**
1. Add memory cache for StyleTemplates (Issue #13)
2. Optimize bounds calculation (Issue #24)
3. Replace magic numbers with constants (Issue #9)
4. Refactor room generation into smaller methods (Issue #10)
5. Fix wall height calculation (Issue #4)

**Success Criteria:**
- StyleTemplate queries cached (faster response)
- Bounds calculated once per floor (faster rendering)
- All magic numbers documented with constants
- Room generation methods < 50 lines each
- Wall heights based on actual roof geometry

### Phase 4: Scalability & Documentation (Days 11-14)
**Goal:** Prepare for production and future expansion

**Tasks:**
1. Add AspectRatio to StyleTemplate (Issue #8)
2. Create admin API for StyleTemplates (Issue #14)
3. Fix layout seed mapping (Issue #7)
4. Update package.json versions (Issue #15)
5. Create coordinate system documentation (Issue #16)
6. Add input validation throughout

**Success Criteria:**
- Different styles have different aspect ratios
- New styles can be added without code changes
- Layout mapping is consistent and validated
- Dependencies match actual versions
- Coordinate systems fully documented

---

## Testing Requirements

### Unit Tests to Add
- `RoofGeometryTests.cs`: Validate pitch calculations
- `CoordinateTransformTests.cs`: Validate room position conversions
- `LayoutBoundsTests.cs`: Validate bounds per layout type
- `MemoryLeakTests.js`: Validate Three.js disposal

### Integration Tests to Add
- Multi-story building rendering (all layouts)
- Story override with interior walls
- OBJ export matching 3D viewer
- Rate limiting and CORS enforcement

### Manual Testing Checklist
- [ ] Generate 3500 sqft Brutalist 1-story: verify roof pitch
- [ ] Generate 3500 sqft Brutalist 3-story override: verify walls on all floors
- [ ] Generate L-shape: verify interior walls don't protrude
- [ ] Generate angled 3-story: verify roof alignment
- [ ] Regenerate 50 times: verify memory stable
- [ ] Try API from different origin: verify CORS blocked
- [ ] Download OBJ: verify matches 3D viewer

---

## Code Changes by File

### Backend Changes

**Program.cs:**
- Replace `AllowAnyOrigin()` with `WithOrigins(allowedOrigins)`
- Add rate limiting middleware
- Add memory cache services

**DesignsController.cs:**
- Add `[HttpPost("generate-with-stories")]` endpoint
- Make `GenerateRoomLayout` layout-aware (check shape parameter)
- Refactor into smaller methods
- Add StyleTemplate caching
- Remove unused calculations (WindowCount, etc.) or mark as optional
- Add input validation

**AppDbContext.cs:**
- Add AspectRatio to StyleTemplate seed data

**ObjExporter.cs:**
- Note: Consider moving export to frontend with Three.js

### Frontend Changes

**HouseViewer3D.js:**
- Line 588: Fix `roofHeight = roofRadius * pitchRatio`
- Lines 297-306: Replace wall height magic numbers with geometry calculation
- Lines 390-455: Move bounds calculation outside room loop
- Lines 874-885: Add complete Three.js disposal
- Throughout: Replace magic numbers with named constants

**MainScreen.js:**
- Add story override warning
- Update API call to use new endpoint if overriding stories
- Update package.json three version to 0.169.0

**New Files:**
- `COORDINATE_SYSTEMS.md`: Document coordinate transformations
- `constants.js`: Export all layout/geometry constants

---

## Acceptance Criteria

### Critical Issues Resolved
- ✅ Roofs match specified pitch mathematically
- ✅ No memory leaks after repeated regeneration
- ✅ API secured with CORS and rate limiting
- ✅ Multi-story buildings have walls on all floors
- ✅ Interior walls stay within building bounds for all layouts

### Performance Improved
- ✅ StyleTemplate queries cached (< 10ms response)
- ✅ Bounds calculation optimized (< 50ms for complex layouts)
- ✅ Frontend scene updates in < 100ms

### Code Quality Improved
- ✅ No magic numbers without explanation
- ✅ All methods < 100 lines
- ✅ 80%+ test coverage
- ✅ Coordinate systems documented
- ✅ All dependencies up to date

### Production Ready
- ✅ Security audit passes
- ✅ Load testing passes (100 concurrent users)
- ✅ All manual test checklist items pass
- ✅ Documentation complete

---

## Risk Mitigation

### Risks During Implementation

1. **Risk:** Breaking existing functionality while fixing issues
   - **Mitigation:** Create comprehensive test suite before changes
   - **Mitigation:** Make changes incrementally with verification

2. **Risk:** Three.js disposal breaks rendering
   - **Mitigation:** Test disposal separately from generation
   - **Mitigation:** Use React strict mode to catch double-mount issues

3. **Risk:** Layout-aware room generation creates mismatches
   - **Mitigation:** Generate test cases for all 5 layouts × 3 story counts
   - **Mitigation:** Visual comparison tool for before/after

4. **Risk:** CORS changes break development workflow
   - **Mitigation:** Keep localhost in allowed origins
   - **Mitigation:** Document setup for new developers

5. **Risk:** Roof height changes affect existing designs
   - **Mitigation:** Add version field to designs
   - **Mitigation:** Support both old and new calculations for backwards compatibility

---

## Success Metrics

### Before Fixes
- Roof pitch error: 45%
- Memory leak: +50MB per regeneration
- API response time: 200-500ms
- CORS: Open to all origins
- Story override: Broken (missing walls)

### After Fixes (Target)
- Roof pitch error: < 1%
- Memory leak: 0 MB growth
- API response time: 50-100ms (with caching)
- CORS: Restricted with rate limiting
- Story override: Working (walls on all floors)

---

## Timeline

**Week 1:** Phase 1 (Critical fixes)
**Week 2:** Phase 2 (Architectural fixes)
**Week 3:** Phase 3 (Performance & quality)
**Week 4:** Phase 4 (Scalability & documentation)

**Total Estimated Time:** 4 weeks (1 developer)

---

## Next Steps

1. Review this prompt with team
2. Set up test environment with metrics collection
3. Create feature branch: `fix/comprehensive-issues`
4. Begin Phase 1: Critical fixes
5. Daily standup to track progress against this plan
