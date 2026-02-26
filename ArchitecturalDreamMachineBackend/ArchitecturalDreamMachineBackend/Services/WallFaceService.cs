using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Generates one exterior wall face panel per face per section, each carrying
/// the window/door openings that belong to that face in face-local 2D coords.
/// The frontend uses THREE.ShapeGeometry with holes to render true cut-outs.
///
/// Overlap holes: when two sections share or overlap a face plane, rectangular
/// holes are punched so the hidden portion is cut away (preventing z-fighting
/// and wall panels sticking through other sections).
/// </summary>
public class WallFaceService : IWallFaceService
{
    // Windows are placed 0.1 ft proud of the wall surface; allow generous tolerance
    private const double PositionTolerance = 1.5;
    // Offset panels slightly outward to prevent Z-fighting with section boxes
    private const double SkinOffset = 0.02;
    // Margin added to overlap holes to prevent z-fighting at overlap edges
    private const double OverlapMargin = 0.1;
    // Tolerance for coplanar face detection
    private const double CoplanarTolerance = 0.01;

    public WallFaceResult GenerateWallFaces(
        List<LayoutSection> sections,
        List<WindowElement>  windows,
        List<DoorElement>    doors,
        string materialType,
        string color)
    {
        var result = new WallFaceResult();

        foreach (var sec in sections)
        {
            double baseY = sec.Y - sec.Height / 2;

            foreach (var dir in new[] { FaceDir.Front, FaceDir.Back, FaceDir.Right, FaceDir.Left })
            {
                if (IsFaceInterior(sec, dir, baseY, sections)) continue;
                result.Faces.Add(BuildFace(sec, baseY, dir, windows, doors,
                    sections, result.PlacedWindowIds, materialType, color));
            }
        }

        return result;
    }

    // ---------------------------------------------------------------------------

    private enum FaceDir { Front, Back, Right, Left }

    private static WallFaceData BuildFace(
        LayoutSection sec,
        double baseY,
        FaceDir dir,
        List<WindowElement> windows,
        List<DoorElement> doors,
        List<LayoutSection> allSections,
        HashSet<string> placedWindowIds,
        string materialType,
        string color)
    {
        var (x, z, width, rotY) = dir switch
        {
            FaceDir.Front => (sec.X,                              sec.Z + sec.Depth  / 2 + SkinOffset, sec.Width, 0.0),
            FaceDir.Back  => (sec.X,                              sec.Z - sec.Depth  / 2 - SkinOffset, sec.Width, Math.PI),
            FaceDir.Right => (sec.X + sec.Width / 2 + SkinOffset, sec.Z,                              sec.Depth, -Math.PI / 2),
            FaceDir.Left  => (sec.X - sec.Width / 2 - SkinOffset, sec.Z,                              sec.Depth,  Math.PI / 2),
            _ => throw new ArgumentOutOfRangeException()
        };

        var face = new WallFaceData
        {
            X = x, Y = baseY, Z = z,
            Width = width, Height = sec.Height,
            RotationY  = rotY,
            MaterialType = materialType,
            Color = color
        };

        // Compute overlap holes where other sections cover part of this face
        var overlapHoles = ComputeOverlapHoles(sec, dir, baseY, allSections);
        face.Openings.AddRange(overlapHoles);

        // Add window openings (skip windows that fall inside overlap zones)
        foreach (var w in windows)
        {
            if (!WindowBelongsToFace(w, sec, dir)) continue;

            double offsetX = ComputeWindowOffsetX(w, sec, dir);
            double offsetY = w.Y - baseY;

            if (WindowInOverlapZone(offsetX, offsetY, overlapHoles)) continue;

            face.Openings.Add(new WallOpeningData
            {
                Type    = "window",
                OffsetX = offsetX,
                OffsetY = offsetY,
                Width   = w.Width,
                Height  = w.Height
            });
            placedWindowIds.Add(w.Id);
        }

        // Add exterior door openings
        foreach (var d in doors)
        {
            if (!d.IsExterior) continue;
            if (!DoorBelongsToFace(d, sec, dir)) continue;

            face.Openings.Add(new WallOpeningData
            {
                Type    = "door",
                OffsetX = ComputeDoorOffsetX(d, sec, dir),
                OffsetY = d.Y - baseY,
                Width   = d.Width,
                Height  = d.Height
            });
        }

        return face;
    }

    // ---------------------------------------------------------------------------
    // Overlap hole computation
    //
    // For each face panel, check every other section. If the other section's body
    // reaches or shares this face's plane, compute the rectangular overlap and
    // add it as a WallOpeningData (type="overlap"). The frontend punches these
    // exactly like window/door holes.

    private static List<WallOpeningData> ComputeOverlapHoles(
        LayoutSection sec, FaceDir dir, double baseY,
        List<LayoutSection> allSections)
    {
        var holes = new List<WallOpeningData>();
        double topY = baseY + sec.Height;

        // Face plane coordinate and spanning axis
        double facePlane;
        bool planeOnZAxis;
        double spanMin, spanMax;

        switch (dir)
        {
            case FaceDir.Front:
                facePlane    = sec.Z + sec.Depth / 2;
                planeOnZAxis = true;
                spanMin = sec.X - sec.Width / 2;
                spanMax = sec.X + sec.Width / 2;
                break;
            case FaceDir.Back:
                facePlane    = sec.Z - sec.Depth / 2;
                planeOnZAxis = true;
                spanMin = sec.X - sec.Width / 2;
                spanMax = sec.X + sec.Width / 2;
                break;
            case FaceDir.Right:
                facePlane    = sec.X + sec.Width / 2;
                planeOnZAxis = false;
                spanMin = sec.Z - sec.Depth / 2;
                spanMax = sec.Z + sec.Depth / 2;
                break;
            case FaceDir.Left:
                facePlane    = sec.X - sec.Width / 2;
                planeOnZAxis = false;
                spanMin = sec.Z - sec.Depth / 2;
                spanMax = sec.Z + sec.Depth / 2;
                break;
            default:
                return holes;
        }

        foreach (var other in allSections)
        {
            if (ReferenceEquals(other, sec)) continue;

            // Height overlap
            double otherBaseY = other.Y - other.Height / 2;
            double otherTopY  = other.Y + other.Height / 2;
            double hMin = Math.Max(baseY, otherBaseY);
            double hMax = Math.Min(topY, otherTopY);
            if (hMax <= hMin) continue;

            // Does the other section's body reach this face's plane?
            double otherPlaneMin, otherPlaneMax;
            double otherSpanMin, otherSpanMax;
            if (planeOnZAxis)
            {
                otherPlaneMin = other.Z - other.Depth / 2;
                otherPlaneMax = other.Z + other.Depth / 2;
                otherSpanMin  = other.X - other.Width / 2;
                otherSpanMax  = other.X + other.Width / 2;
            }
            else
            {
                otherPlaneMin = other.X - other.Width / 2;
                otherPlaneMax = other.X + other.Width / 2;
                otherSpanMin  = other.Z - other.Depth / 2;
                otherSpanMax  = other.Z + other.Depth / 2;
            }

            bool strictlyInside = facePlane > otherPlaneMin + CoplanarTolerance
                               && facePlane < otherPlaneMax - CoplanarTolerance;
            bool coplanar = !strictlyInside
                         && facePlane >= otherPlaneMin - CoplanarTolerance
                         && facePlane <= otherPlaneMax + CoplanarTolerance;

            if (!strictlyInside && !coplanar) continue;

            // Coplanar tiebreaker: taller section wins (gets to keep its face).
            // The shorter section gets a hole punched in the overlapping area.
            if (coplanar)
            {
                if (otherTopY < topY - CoplanarTolerance) continue;  // other is shorter → we keep face
                if (Math.Abs(otherTopY - topY) < CoplanarTolerance   // same top height
                    && otherBaseY <= baseY + CoplanarTolerance)       // other starts same/lower → we keep
                    continue;
            }

            // Span overlap along the face's width axis
            double sMin = Math.Max(spanMin, otherSpanMin);
            double sMax = Math.Min(spanMax, otherSpanMax);
            if (sMax <= sMin) continue;

            // Expand overlap by margin, then clamp to face bounds
            double holeYMin = Math.Max(hMin - OverlapMargin, baseY);
            double holeYMax = Math.Min(hMax + OverlapMargin, topY);
            double holeSMin = Math.Max(sMin - OverlapMargin, spanMin);
            double holeSMax = Math.Min(sMax + OverlapMargin, spanMax);

            double holeWidth  = holeSMax - holeSMin;
            double holeHeight = holeYMax - holeYMin;
            if (holeWidth <= 0 || holeHeight <= 0) continue;

            double holeCenterSpan = (holeSMin + holeSMax) / 2;
            double holeCenterY    = (holeYMin + holeYMax) / 2;

            double offsetX = dir switch
            {
                FaceDir.Front => holeCenterSpan - sec.X,
                FaceDir.Back  => sec.X - holeCenterSpan,
                FaceDir.Right => holeCenterSpan - sec.Z,
                FaceDir.Left  => sec.Z - holeCenterSpan,
                _ => 0
            };

            holes.Add(new WallOpeningData
            {
                Type    = "overlap",
                OffsetX = offsetX,
                OffsetY = holeCenterY - baseY,
                Width   = holeWidth,
                Height  = holeHeight
            });
        }

        return holes;
    }

    /// <summary>
    /// Returns true if the window's face-local center falls inside any overlap hole.
    /// </summary>
    private static bool WindowInOverlapZone(
        double winOffsetX, double winOffsetY,
        List<WallOpeningData> overlapHoles)
    {
        foreach (var hole in overlapHoles)
        {
            if (hole.Type != "overlap") continue;

            double left   = hole.OffsetX - hole.Width  / 2;
            double right  = hole.OffsetX + hole.Width  / 2;
            double bottom = hole.OffsetY - hole.Height / 2;
            double top    = hole.OffsetY + hole.Height / 2;

            if (winOffsetX >= left && winOffsetX <= right
                && winOffsetY >= bottom && winOffsetY <= top)
            {
                return true;
            }
        }
        return false;
    }

    // ---------------------------------------------------------------------------
    // Window matching

    private static bool WindowBelongsToFace(WindowElement w, LayoutSection sec, FaceDir dir)
    {
        // Check if window Y is within section's vertical range
        // (sections can span multiple floors, so we check Y position instead of floor number)
        double secMinY = sec.Y - sec.Height / 2;
        double secMaxY = sec.Y + sec.Height / 2;
        if (w.Y < secMinY - PositionTolerance || w.Y > secMaxY + PositionTolerance) return false;

        return dir switch
        {
            FaceDir.Front => w.WallDirection == WallDirection.Front
                             && Math.Abs(w.Z - (sec.Z + sec.Depth / 2)) < PositionTolerance
                             && w.X >= sec.X - sec.Width / 2 - PositionTolerance
                             && w.X <= sec.X + sec.Width / 2 + PositionTolerance,

            FaceDir.Back  => w.WallDirection == WallDirection.Back
                             && Math.Abs(w.Z - (sec.Z - sec.Depth / 2)) < PositionTolerance
                             && w.X >= sec.X - sec.Width / 2 - PositionTolerance
                             && w.X <= sec.X + sec.Width / 2 + PositionTolerance,

            FaceDir.Right => w.WallDirection == WallDirection.Right
                             && Math.Abs(w.X - (sec.X + sec.Width / 2)) < PositionTolerance
                             && w.Z >= sec.Z - sec.Depth / 2 - PositionTolerance
                             && w.Z <= sec.Z + sec.Depth / 2 + PositionTolerance,

            FaceDir.Left  => w.WallDirection == WallDirection.Left
                             && Math.Abs(w.X - (sec.X - sec.Width / 2)) < PositionTolerance
                             && w.Z >= sec.Z - sec.Depth / 2 - PositionTolerance
                             && w.Z <= sec.Z + sec.Depth / 2 + PositionTolerance,

            _ => false
        };
    }

    private static double ComputeWindowOffsetX(WindowElement w, LayoutSection sec, FaceDir dir) =>
        dir switch
        {
            FaceDir.Front => w.X - sec.X,       // local +X = world +X
            FaceDir.Back  => sec.X - w.X,       // panel rotated π → local +X = world -X
            FaceDir.Right => w.Z - sec.Z,       // panel rotated -π/2 → local +X = world +Z
            FaceDir.Left  => sec.Z - w.Z,       // panel rotated +π/2 → local +X = world -Z
            _ => 0
        };

    // ---------------------------------------------------------------------------
    // Door matching (use wall orientation + position to identify face)

    private static bool DoorBelongsToFace(DoorElement d, LayoutSection sec, FaceDir dir)
    {
        // Check if door Y is within section's vertical range (like windows)
        double secMinY = sec.Y - sec.Height / 2;
        double secMaxY = sec.Y + sec.Height / 2;
        if (d.Y < secMinY - PositionTolerance || d.Y > secMaxY + PositionTolerance) return false;

        return dir switch
        {
            FaceDir.Front => d.WallOrientation == DoorWallOrientation.Horizontal
                             && Math.Abs(d.Z - (sec.Z + sec.Depth / 2)) < PositionTolerance
                             && d.X >= sec.X - sec.Width / 2 - PositionTolerance
                             && d.X <= sec.X + sec.Width / 2 + PositionTolerance,

            FaceDir.Back  => d.WallOrientation == DoorWallOrientation.Horizontal
                             && Math.Abs(d.Z - (sec.Z - sec.Depth / 2)) < PositionTolerance
                             && d.X >= sec.X - sec.Width / 2 - PositionTolerance
                             && d.X <= sec.X + sec.Width / 2 + PositionTolerance,

            FaceDir.Right => d.WallOrientation == DoorWallOrientation.Vertical
                             && Math.Abs(d.X - (sec.X + sec.Width / 2)) < PositionTolerance
                             && d.Z >= sec.Z - sec.Depth / 2 - PositionTolerance
                             && d.Z <= sec.Z + sec.Depth / 2 + PositionTolerance,

            FaceDir.Left  => d.WallOrientation == DoorWallOrientation.Vertical
                             && Math.Abs(d.X - (sec.X - sec.Width / 2)) < PositionTolerance
                             && d.Z >= sec.Z - sec.Depth / 2 - PositionTolerance
                             && d.Z <= sec.Z + sec.Depth / 2 + PositionTolerance,

            _ => false
        };
    }

    private static double ComputeDoorOffsetX(DoorElement d, LayoutSection sec, FaceDir dir) =>
        dir switch
        {
            FaceDir.Front => d.X - sec.X,
            FaceDir.Back  => sec.X - d.X,
            FaceDir.Right => d.Z - sec.Z,
            FaceDir.Left  => sec.Z - d.Z,
            _ => 0
        };

    // ---------------------------------------------------------------------------
    // Interior-face suppression (fast path)
    //
    // A face is considered fully interior (and should not be rendered at all) when:
    //   1. ≥50% of the face's height is co-planar with another section
    //   2. The face plane is strictly inside that section on the same axis
    //   3. ≥35% of the face's width-span overlaps that section
    //
    // Faces that pass this check but are partially overlapped get overlap holes
    // via ComputeOverlapHoles instead.

    private static bool IsFaceInterior(
        LayoutSection sec, FaceDir dir, double baseY,
        List<LayoutSection> allSections)
    {
        double facePlane;
        bool   planeOnZAxis;
        double spanMin, spanMax;

        switch (dir)
        {
            case FaceDir.Front:
                facePlane    = sec.Z + sec.Depth / 2;
                planeOnZAxis = true;
                spanMin = sec.X - sec.Width / 2;
                spanMax = sec.X + sec.Width / 2;
                break;
            case FaceDir.Back:
                facePlane    = sec.Z - sec.Depth / 2;
                planeOnZAxis = true;
                spanMin = sec.X - sec.Width / 2;
                spanMax = sec.X + sec.Width / 2;
                break;
            case FaceDir.Right:
                facePlane    = sec.X + sec.Width / 2;
                planeOnZAxis = false;
                spanMin = sec.Z - sec.Depth / 2;
                spanMax = sec.Z + sec.Depth / 2;
                break;
            case FaceDir.Left:
                facePlane    = sec.X - sec.Width / 2;
                planeOnZAxis = false;
                spanMin = sec.Z - sec.Depth / 2;
                spanMax = sec.Z + sec.Depth / 2;
                break;
            default:
                return false;
        }

        double faceHeight = sec.Height;
        double faceSpan   = spanMax - spanMin;
        double topY       = baseY + faceHeight;

        foreach (var other in allSections)
        {
            if (ReferenceEquals(other, sec)) continue;

            // 1. Height-overlap fraction must be ≥ 50 %
            double otherBaseY    = other.Y - other.Height / 2;
            double otherTopY     = other.Y + other.Height / 2;
            double hOverlap      = Math.Min(topY, otherTopY) - Math.Max(baseY, otherBaseY);
            if (hOverlap <= 0) continue;
            if (hOverlap / faceHeight < 0.5) continue;

            // 2. Face plane must be STRICTLY inside the other section on the same axis
            double otherPlaneMin, otherPlaneMax;
            double otherSpanMin,  otherSpanMax;
            if (planeOnZAxis)
            {
                otherPlaneMin = other.Z - other.Depth / 2;
                otherPlaneMax = other.Z + other.Depth / 2;
                otherSpanMin  = other.X - other.Width / 2;
                otherSpanMax  = other.X + other.Width / 2;
            }
            else
            {
                otherPlaneMin = other.X - other.Width  / 2;
                otherPlaneMax = other.X + other.Width  / 2;
                otherSpanMin  = other.Z - other.Depth  / 2;
                otherSpanMax  = other.Z + other.Depth  / 2;
            }

            if (facePlane <= otherPlaneMin || facePlane >= otherPlaneMax) continue;

            // 3. Width-span overlap fraction must be ≥ 35 %
            double sOverlap = Math.Min(spanMax, otherSpanMax) - Math.Max(spanMin, otherSpanMin);
            if (sOverlap <= 0) continue;
            if (sOverlap / faceSpan < 0.35) continue;

            return true; // face is interior to another section — suppress it
        }

        return false;
    }
}
