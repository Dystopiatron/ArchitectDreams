using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;

namespace ArchitecturalDreamMachineBackend.Services;

public interface IWallFaceService
{
    /// <summary>
    /// Generate perforated wall face panels for all exterior faces of every
    /// building section.  Each panel carries the 2D opening data (windows and
    /// exterior doors) needed by the Three.js ShapeGeometry renderer.
    /// </summary>
    List<WallFaceData> GenerateWallFaces(
        List<LayoutSection> sections,
        List<WindowElement>  windows,
        List<DoorElement>    doors,
        string materialType,
        string color);
}
