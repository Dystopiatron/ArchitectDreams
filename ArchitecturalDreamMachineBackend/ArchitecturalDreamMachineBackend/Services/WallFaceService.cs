using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

/// <summary>
/// Generates one exterior wall face panel per face per section, each carrying
/// the window/door openings that belong to that face in face-local 2D coords.
/// The frontend uses THREE.ShapeGeometry with holes to render true cut-outs.
/// </summary>
public class WallFaceService : IWallFaceService
{
    // Windows are placed 0.1 ft proud of the wall surface; allow generous tolerance
    private const double PositionTolerance = 1.5;
    // Offset panels slightly outward to prevent Z-fighting with section boxes
    private const double SkinOffset = 0.02;

    public List<WallFaceData> GenerateWallFaces(
        List<LayoutSection> sections,
        List<WindowElement>  windows,
        List<DoorElement>    doors,
        string materialType,
        string color)
    {
        var faces = new List<WallFaceData>();

        foreach (var sec in sections)
        {
            double baseY = sec.Y - sec.Height / 2;

            foreach (var dir in new[] { FaceDir.Front, FaceDir.Back, FaceDir.Right, FaceDir.Left })
            {
                if (IsFaceInterior(sec, dir, baseY, sections)) continue;
                faces.Add(BuildFace(sec, baseY, dir, windows, doors, materialType, color));
            }
        }

        return faces;
    }

    // ---------------------------------------------------------------------------

    private enum FaceDir { Front, Back, Right, Left }

    private static WallFaceData BuildFace(
        LayoutSection sec,
        double baseY,
        FaceDir dir,
        List<WindowElement> windows,
        List<DoorElement> doors,
        string materialType,
        string color)
    {
        var (x, z, width, rotY) = dir switch
        {
            FaceDir.Front => (sec.X,                          sec.Z + sec.Depth  / 2 + SkinOffset, sec.Width, 0.0),
            FaceDir.Back  => (sec.X,                          sec.Z - sec.Depth  / 2 - SkinOffset, sec.Width, Math.PI),
            FaceDir.Right => (sec.X + sec.Width / 2 + SkinOffset, sec.Z,                          sec.Depth, -Math.PI / 2),
            FaceDir.Left  => (sec.X - sec.Width / 2 - SkinOffset, sec.Z,                          sec.Depth,  Math.PI / 2),
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

        // Add window openings
        foreach (var w in windows)
        {
            if (!WindowBelongsToFace(w, sec, dir)) continue;

            face.Openings.Add(new WallOpeningData
            {
                Type    = "window",
                OffsetX = ComputeWindowOffsetX(w, sec, dir),
                OffsetY = w.Y - baseY,
                Width   = w.Width,
                Height  = w.Height
            });
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
    // Window matching

    private static bool WindowBelongsToFace(WindowElement w, LayoutSection sec, FaceDir dir)
    {
        if (w.Floor != sec.Floor) return false;

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
        if (d.Floor != sec.Floor) return false;

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
    // Interior-face suppression
    //
    // A face is considered interior (and should not be rendered) when all of:
    //   1. ≥50% of the face's height is co-planar with another section
    //   2. The face plane is strictly inside that section on the same axis
    //   3. ≥40% of the face's width-span overlaps that section
    //
    // This removes wing/extension faces that physically cut through the main
    // section body (e.g. angled-modern's wing Left/Back faces) while preserving
    // L-shape step faces (small span overlap <40%) and multi-story tower faces
    // where the wing only covers 1 of 3 floors (height fraction <50%).

    private static bool IsFaceInterior(
        LayoutSection sec, FaceDir dir, double baseY,
        List<LayoutSection> allSections)
    {
        // Compute face plane value, spanning axis, and face width range
        double facePlane;
        bool   planeOnZAxis; // true = Front/Back (Z-value plane), false = Left/Right (X-value plane)
        double spanMin, spanMax;

        switch (dir)
        {
            case FaceDir.Front:
                facePlane   = sec.Z + sec.Depth / 2;
                planeOnZAxis = true;
                spanMin = sec.X - sec.Width / 2;
                spanMax = sec.X + sec.Width / 2;
                break;
            case FaceDir.Back:
                facePlane   = sec.Z - sec.Depth / 2;
                planeOnZAxis = true;
                spanMin = sec.X - sec.Width / 2;
                spanMax = sec.X + sec.Width / 2;
                break;
            case FaceDir.Right:
                facePlane   = sec.X + sec.Width / 2;
                planeOnZAxis = false;
                spanMin = sec.Z - sec.Depth / 2;
                spanMax = sec.Z + sec.Depth / 2;
                break;
            case FaceDir.Left:
                facePlane   = sec.X - sec.Width / 2;
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

            // 3. Width-span overlap fraction must be ≥ 40 %
            double sOverlap = Math.Min(spanMax, otherSpanMax) - Math.Max(spanMin, otherSpanMin);
            if (sOverlap <= 0) continue;
            if (sOverlap / faceSpan < 0.4) continue;

            return true; // face is interior to another section — suppress it
        }

        return false;
    }
}
