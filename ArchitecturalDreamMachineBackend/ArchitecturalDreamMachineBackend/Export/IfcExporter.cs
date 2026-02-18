using ArchitecturalDreamMachineBackend.Geometry;
using ArchitecturalDreamMachineBackend.Models;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.IO;

namespace ArchitecturalDreamMachineBackend.Export;

/// <summary>
/// Service for exporting building designs to IFC format
/// Uses xBIM toolkit for IFC4 generation
/// </summary>
public class IfcExporter : IIfcExporter
{
    private readonly ILogger<IfcExporter> _logger;

    public IfcExporter(ILogger<IfcExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public byte[] ExportToIfc(HouseParameters parameters, BuildingGeometry geometry, string styleName)
    {
        _logger.LogInformation(
            "Exporting to IFC: style={Style}, {Stories} stories, {Rooms} rooms",
            styleName, parameters.Stories, parameters.Rooms.Count);

        using var model = CreateModel();
        
        using (var txn = model.BeginTransaction("Create Building"))
        {
            // Create project structure
            var project = CreateProject(model, styleName);
            var site = CreateSite(model, project);
            var building = CreateBuilding(model, site, parameters, styleName);
            
            // Create building storeys
            var storeys = CreateStoreys(model, building, parameters);
            
            // Create spaces (rooms) for each storey
            CreateSpaces(model, storeys, parameters.Rooms, parameters.CeilingHeight);
            
            // Create floor and ceiling slabs (new BIM-compliant method)
            CreateFloorSlabs(model, storeys, parameters);
            
            // Create exterior walls from sections — returns dict for window host-wall linking
            var exteriorWallsByDirection = CreateExteriorWalls(model, storeys, geometry.Sections, parameters);

            // Create windows with proper openings (new BIM-compliant method)
            if (geometry.WindowElements.Any())
            {
                CreateWindowsWithOpenings(model, storeys, geometry.WindowElements, parameters.CeilingHeight, exteriorWallsByDirection);
            }
            else
            {
                // Fallback to legacy window creation
                CreateWindows(model, storeys, geometry.Windows, parameters.CeilingHeight);
            }

            // Create interior walls — returns records for door host-wall matching
            var interiorWallRecords = CreateInteriorWalls(model, storeys, geometry.InteriorWalls, parameters.CeilingHeight);

            // Create doors with proper openings (new BIM-compliant method)
            if (geometry.DoorElements.Any())
            {
                CreateDoorsWithOpenings(model, storeys, geometry.DoorElements, parameters.CeilingHeight, interiorWallRecords);
            }
            
            // Create roof
            CreateRoof(model, storeys.Last(), geometry.Roofs, parameters);
            
            // Add property sets with architectural parameters
            AddBuildingProperties(model, building, parameters);
            
            txn.Commit();
        }
        
        // Export to byte array
        using var ms = new MemoryStream();
        model.SaveAsIfc(ms);
        
        _logger.LogInformation("IFC export complete: {Size} bytes", ms.Length);
        return ms.ToArray();
    }

    private IfcStore CreateModel()
    {
        var credentials = new XbimEditorCredentials
        {
            ApplicationDevelopersName = "Architectural Dream Machine",
            ApplicationFullName = "Architectural Dream Machine",
            ApplicationIdentifier = "ADM",
            ApplicationVersion = "1.0",
            EditorsFamilyName = "User",
            EditorsGivenName = "System",
            EditorsOrganisationName = "Generated"
        };
        
        return IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
    }

    private IfcProject CreateProject(IfcStore model, string styleName)
    {
        var project = model.Instances.New<IfcProject>(p =>
        {
            p.Name = $"Architectural Dream Machine - {styleName}";
            p.Description = "Generated architectural design";
            p.UnitsInContext = CreateUnitAssignment(model);
        });
        
        // Create geometric context
        var context3D = model.Instances.New<IfcGeometricRepresentationContext>(c =>
        {
            c.ContextType = "Model";
            c.CoordinateSpaceDimension = 3;
            c.Precision = 0.00001;
            c.WorldCoordinateSystem = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
        });
        project.RepresentationContexts.Add(context3D);

        // Create sub-contexts for Body (solid geometry), Axis (centrelines), Box (bounding box)
        model.Instances.New<IfcGeometricRepresentationSubContext>(c =>
        {
            c.ParentContext = context3D;
            c.ContextIdentifier = "Body";
            c.ContextType = "Model";
            c.TargetView = IfcGeometricProjectionEnum.MODEL_VIEW;
        });
        model.Instances.New<IfcGeometricRepresentationSubContext>(c =>
        {
            c.ParentContext = context3D;
            c.ContextIdentifier = "Axis";
            c.ContextType = "Model";
            c.TargetView = IfcGeometricProjectionEnum.GRAPH_VIEW;
        });
        model.Instances.New<IfcGeometricRepresentationSubContext>(c =>
        {
            c.ParentContext = context3D;
            c.ContextIdentifier = "Box";
            c.ContextType = "Model";
            c.TargetView = IfcGeometricProjectionEnum.MODEL_VIEW;
        });

        return project;
    }

    private IfcUnitAssignment CreateUnitAssignment(IfcStore model)
    {
        // Use imperial units (feet) to match our model
        var unitAssignment = model.Instances.New<IfcUnitAssignment>();
        
        // Length in feet
        var lengthUnit = model.Instances.New<IfcConversionBasedUnit>(u =>
        {
            u.Name = "foot";
            u.UnitType = IfcUnitEnum.LENGTHUNIT;
            u.Dimensions = model.Instances.New<IfcDimensionalExponents>(d =>
            {
                d.LengthExponent = 1;
            });
            u.ConversionFactor = model.Instances.New<IfcMeasureWithUnit>(m =>
            {
                m.ValueComponent = new IfcRatioMeasure(0.3048); // feet to meters
                m.UnitComponent = model.Instances.New<IfcSIUnit>(si =>
                {
                    si.UnitType = IfcUnitEnum.LENGTHUNIT;
                    si.Name = IfcSIUnitName.METRE;
                });
            });
        });
        unitAssignment.Units.Add(lengthUnit);
        
        // Area in square feet
        var areaUnit = model.Instances.New<IfcConversionBasedUnit>(u =>
        {
            u.Name = "square foot";
            u.UnitType = IfcUnitEnum.AREAUNIT;
            u.Dimensions = model.Instances.New<IfcDimensionalExponents>(d =>
            {
                d.LengthExponent = 2;
            });
            u.ConversionFactor = model.Instances.New<IfcMeasureWithUnit>(m =>
            {
                m.ValueComponent = new IfcRatioMeasure(0.09290304); // sq ft to sq m
                m.UnitComponent = model.Instances.New<IfcSIUnit>(si =>
                {
                    si.UnitType = IfcUnitEnum.AREAUNIT;
                    si.Name = IfcSIUnitName.SQUARE_METRE;
                });
            });
        });
        unitAssignment.Units.Add(areaUnit);
        
        // Plane angle in degrees
        var angleUnit = model.Instances.New<IfcSIUnit>(u =>
        {
            u.UnitType = IfcUnitEnum.PLANEANGLEUNIT;
            u.Name = IfcSIUnitName.RADIAN;
        });
        unitAssignment.Units.Add(angleUnit);
        
        return unitAssignment;
    }

    private IfcSite CreateSite(IfcStore model, IfcProject project)
    {
        var site = model.Instances.New<IfcSite>(s =>
        {
            s.Name = "Default Site";
            s.CompositionType = IfcElementCompositionEnum.ELEMENT;
        });
        
        // Link site to project
        model.Instances.New<IfcRelAggregates>(r =>
        {
            r.RelatingObject = project;
            r.RelatedObjects.Add(site);
        });
        
        return site;
    }

    private IfcBuilding CreateBuilding(IfcStore model, IfcSite site, HouseParameters parameters, string styleName)
    {
        var building = model.Instances.New<IfcBuilding>(b =>
        {
            b.Name = $"{styleName} House";
            b.Description = $"Generated {parameters.BuildingShape} style building";
            b.CompositionType = IfcElementCompositionEnum.ELEMENT;
            b.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
            {
                lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                {
                    a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                });
            });
        });
        
        // Link building to site
        model.Instances.New<IfcRelAggregates>(r =>
        {
            r.RelatingObject = site;
            r.RelatedObjects.Add(building);
        });
        
        return building;
    }

    private List<IfcBuildingStorey> CreateStoreys(IfcStore model, IfcBuilding building, HouseParameters parameters)
    {
        var storeys = new List<IfcBuildingStorey>();
        
        for (int floor = 1; floor <= parameters.Stories; floor++)
        {
            double elevation = (floor - 1) * parameters.CeilingHeight;
            
            var storey = model.Instances.New<IfcBuildingStorey>(s =>
            {
                s.Name = $"Level {floor}";
                s.Elevation = elevation;
                s.CompositionType = IfcElementCompositionEnum.ELEMENT;
                s.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = building.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, elevation));
                    });
                });
            });
            
            storeys.Add(storey);
        }
        
        // Link storeys to building
        model.Instances.New<IfcRelAggregates>(r =>
        {
            r.RelatingObject = building;
            r.RelatedObjects.AddRange(storeys);
        });
        
        return storeys;
    }

    private void CreateSpaces(IfcStore model, List<IfcBuildingStorey> storeys, List<Room> rooms, double ceilingHeight)
    {
        foreach (var room in rooms)
        {
            if (room.Floor < 1 || room.Floor > storeys.Count) continue;
            
            var storey = storeys[room.Floor - 1];
            
            var space = model.Instances.New<IfcSpace>(s =>
            {
                s.Name = room.Name;
                s.LongName = room.Name;
                s.CompositionType = IfcElementCompositionEnum.ELEMENT;
                s.PredefinedType = IfcSpaceTypeEnum.INTERNAL;
                s.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = storey.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => 
                            p.SetXYZ(room.X, room.Z, 0)); // XY plane in IFC, Z is up
                    });
                });
            });
            
            // Create simple box representation for the space
            var representation = CreateBoxRepresentation(model, room.Width, room.Depth, ceilingHeight);
            space.Representation = representation;
            
            // Link space to storey
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(space);
            });
            
            // Add room properties
            AddSpaceProperties(model, space, room);
        }
    }

    /// <summary>
    /// Create exterior walls with proper line-based geometry and extrusion.
    /// Returns a dictionary keyed by (WallDirection, floorIndex) for window host-wall linking.
    /// Phase 1.4: Replace section-based walls with properly extruded wall geometry
    /// </summary>
    private Dictionary<(WallDirection direction, int floorIndex), IfcWallStandardCase> CreateExteriorWalls(
        IfcStore model, List<IfcBuildingStorey> storeys,
        List<GeometryData> sections, HouseParameters parameters)
    {
        double wallThickness = 0.5; // 6 inches in feet
        double wallHeight = parameters.CeilingHeight;

        // Ordered to match WallDirection enum: Front=0, Back=1, Left=2, Right=3
        var perimeterWalls = new List<(double x1, double z1, double x2, double z2, string name, WallDirection direction)>
        {
            (0, 0, parameters.FootprintWidth, 0, "Front Wall", WallDirection.Front),
            (0, parameters.FootprintDepth, parameters.FootprintWidth, parameters.FootprintDepth, "Back Wall", WallDirection.Back),
            (0, 0, 0, parameters.FootprintDepth, "Left Wall", WallDirection.Left),
            (parameters.FootprintWidth, 0, parameters.FootprintWidth, parameters.FootprintDepth, "Right Wall", WallDirection.Right)
        };

        var wallsByDirectionAndFloor = new Dictionary<(WallDirection, int), IfcWallStandardCase>();

        for (int floorIndex = 0; floorIndex < storeys.Count; floorIndex++)
        {
            var storey = storeys[floorIndex];

            foreach (var (x1, z1, x2, z2, name, direction) in perimeterWalls)
            {
                var wall = CreateProperWall(model, x1, z1, x2, z2,
                    wallHeight, wallThickness, storey, $"{name} - Floor {floorIndex + 1}", true);

                AddWallProperties(model, wall, true, wallThickness);

                wallsByDirectionAndFloor[(direction, floorIndex)] = wall;
            }
        }

        return wallsByDirectionAndFloor;
    }
    
    /// <summary>
    /// Create a proper wall with start/end points, extrusion geometry, and thickness
    /// This follows IfcOpenShell's add_wall_representation pattern
    /// </summary>
    private IfcWallStandardCase CreateProperWall(IfcStore model, double x1, double z1, double x2, double z2,
        double height, double thickness, IfcBuildingStorey storey, string name, bool isExternal)
    {
        // Calculate wall direction and length
        double length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
        double angle = Math.Atan2(z2 - z1, x2 - x1);
        
        // Create wall entity
        var wall = model.Instances.New<IfcWallStandardCase>(w =>
        {
            w.Name = name;
            w.ObjectType = isExternal ? "ExteriorWall" : "InteriorWall";
            w.PredefinedType = IfcWallTypeEnum.STANDARD;
            w.ObjectPlacement = CreateWallPlacement(model, x1, z1, angle, storey);
        });
        
        // Create wall representation with proper extrusion
        var representation = CreateWallRepresentation(model, length, height, thickness);
        wall.Representation = representation;
        
        // Create axis representation (wall centerline)
        AddWallAxisRepresentation(model, wall, length);
        
        // Link wall to storey
        model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
        {
            r.RelatingStructure = storey;
            r.RelatedElements.Add(wall);
        });
        
        return wall;
    }
    
    /// <summary>
    /// Create wall placement with position and rotation
    /// </summary>
    private IfcLocalPlacement CreateWallPlacement(IfcStore model, double x, double z, double angle, IfcBuildingStorey storey)
    {
        return model.Instances.New<IfcLocalPlacement>(lp =>
        {
            lp.PlacementRelTo = storey.ObjectPlacement;
            lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                // Position at wall start point (IFC uses X,Y,Z where Z is up)
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(x, z, 0));
                
                // Set wall direction if not axis-aligned
                if (Math.Abs(angle) > 0.001)
                {
                    // RefDirection is the X direction of the local coordinate system
                    a.RefDirection = model.Instances.New<IfcDirection>(d => 
                        d.SetXY(Math.Cos(angle), Math.Sin(angle)));
                }
            });
        });
    }
    
    /// <summary>
    /// Create wall body representation using extruded area solid
    /// </summary>
    private IfcProductDefinitionShape CreateWallRepresentation(IfcStore model, double length, double height, double thickness)
    {
        var context = model.Instances.OfType<IfcGeometricRepresentationSubContext>()
            .FirstOrDefault(c => c.ContextIdentifier == "Body") ??
            model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
        
        if (context == null) return null!;
        
        // Create rectangular profile for wall cross-section
        // Wall is extruded along its length, profile is thickness x height
        var profile = model.Instances.New<IfcRectangleProfileDef>(p =>
        {
            p.ProfileType = IfcProfileTypeEnum.AREA;
            p.XDim = length;          // Wall length
            p.YDim = thickness;       // Wall thickness
            p.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                // Center the profile at half-length (along wall) and half-thickness (through wall)
                a.Location = model.Instances.New<IfcCartesianPoint>(pt => 
                    pt.SetXY(length / 2, 0));
            });
        });
        
        // Extrude upward (Z direction) to create wall solid
        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = profile;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = height;
        });
        
        var shapeRep = model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
        
        return model.Instances.New<IfcProductDefinitionShape>(pds =>
        {
            pds.Representations.Add(shapeRep);
        });
    }
    
    /// <summary>
    /// Add axis representation (centerline) to wall - useful for BIM analysis
    /// </summary>
    private void AddWallAxisRepresentation(IfcStore model, IfcWallStandardCase wall, double length)
    {
        var axisContext = model.Instances.OfType<IfcGeometricRepresentationSubContext>()
            .FirstOrDefault(c => c.ContextIdentifier == "Axis") ??
            model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
        
        if (axisContext == null) return;
        
        // Create polyline for wall axis (centerline)
        var axis = model.Instances.New<IfcPolyline>(pl =>
        {
            pl.Points.Add(model.Instances.New<IfcCartesianPoint>(p => p.SetXY(0, 0)));
            pl.Points.Add(model.Instances.New<IfcCartesianPoint>(p => p.SetXY(length, 0)));
        });
        
        var axisRep = model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = axisContext;
            sr.RepresentationIdentifier = "Axis";
            sr.RepresentationType = "Curve2D";
            sr.Items.Add(axis);
        });
        
        // Add axis representation to wall
        if (wall.Representation is IfcProductDefinitionShape pds)
        {
            pds.Representations.Add(axisRep);
        }
    }
    
    /// <summary>
    /// Add wall common properties per IFC standards
    /// </summary>
    private void AddWallProperties(IfcStore model, IfcWallStandardCase wall, bool isExternal, double thickness)
    {
        var pset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "Pset_WallCommon";
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "IsExternal";
                p.NominalValue = new IfcBoolean(isExternal);
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "LoadBearing";
                p.NominalValue = new IfcBoolean(isExternal); // Exterior walls are load bearing
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "Reference";
                p.NominalValue = new IfcIdentifier(isExternal ? "EXT-WALL-01" : "INT-WALL-01");
            }));
        });
        
        var admPset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "ADM_WallProperties";
            ps.HasProperties.Add(CreateProperty(model, "WallType", isExternal ? "Exterior" : "Interior"));
            ps.HasProperties.Add(CreateProperty(model, "Thickness", thickness));
            ps.HasProperties.Add(CreateProperty(model, "Material", isExternal ? "Stucco" : "Drywall"));
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = pset;
            r.RelatedObjects.Add(wall);
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = admPset;
            r.RelatedObjects.Add(wall);
        });
    }

    private void CreateWindows(IfcStore model, List<IfcBuildingStorey> storeys, 
        List<GeometryData> windows, double ceilingHeight)
    {
        int windowIndex = 1;
        
        foreach (var windowData in windows)
        {
            if (windowData.Position == null) continue;
            
            // Determine floor from Y position
            int floorIndex = (int)(windowData.Position.Y / ceilingHeight);
            if (floorIndex < 0 || floorIndex >= storeys.Count) floorIndex = 0;
            
            var storey = storeys[floorIndex];
            
            var window = model.Instances.New<IfcWindow>(w =>
            {
                w.Name = $"Window {windowIndex++}";
                w.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = storey.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        double relativeY = windowData.Position.Y - (floorIndex * ceilingHeight);
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => 
                            p.SetXYZ(windowData.Position.X, windowData.Position.Z, relativeY));
                    });
                });
                w.OverallHeight = 4.0; // 4 feet standard
                w.OverallWidth = 3.0;  // 3 feet standard
            });
            
            // Link window to storey
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(window);
            });
        }
    }

    /// <summary>
    /// Create windows with proper IFC opening elements and relationships
    /// This follows the Wall-Opening-Filling pattern for BIM compliance
    /// </summary>
    private void CreateWindowsWithOpenings(IfcStore model, List<IfcBuildingStorey> storeys,
        List<WindowElement> windowElements, double ceilingHeight,
        Dictionary<(WallDirection direction, int floorIndex), IfcWallStandardCase> exteriorWallsByDirection)
    {
        _logger.LogInformation("Creating {Count} windows with proper opening elements", windowElements.Count);
        
        // Get the 3D context for geometry
        var context = model.Instances.OfType<IfcGeometricRepresentationContext>()
            .FirstOrDefault(c => c.ContextType == "Model");
        
        if (context == null)
        {
            _logger.LogWarning("No geometric context found, cannot create window openings");
            return;
        }

        foreach (var windowElement in windowElements)
        {
            // Determine floor from Y position
            int floorIndex = windowElement.Floor - 1;
            if (floorIndex < 0 || floorIndex >= storeys.Count) floorIndex = 0;
            
            var storey = storeys[floorIndex];
            double floorBaseY = floorIndex * ceilingHeight;
            
            // Calculate relative position within the storey
            double relativeY = windowElement.SillHeight;
            
            // Step 1: Create the opening element (the void in the wall)
            var opening = model.Instances.New<IfcOpeningElement>(o =>
            {
                o.Name = $"Opening for {windowElement.Name}";
                o.Description = $"Window opening in {windowElement.WallDirection} wall of {windowElement.RoomName}";
                o.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = storey.ObjectPlacement;
                    lp.RelativePlacement = CreateWindowPlacement(model, windowElement, relativeY);
                });
                o.Representation = CreateOpeningRepresentation(model, context,
                    windowElement.Width, windowElement.Height, windowElement.WallThickness);
                o.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
            });
            
            // Step 2: Create the window element
            var window = model.Instances.New<IfcWindow>(w =>
            {
                w.Name = windowElement.Name;
                w.Description = $"Window in {windowElement.RoomName}";
                w.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = opening.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        // Window is positioned within the opening
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                    });
                });
                w.OverallHeight = windowElement.Height;
                w.OverallWidth = windowElement.Width;
                w.PredefinedType = IfcWindowTypeEnum.WINDOW;
                w.PartitioningType = IfcWindowTypePartitioningEnum.SINGLE_PANEL;
            });
            
            // Add window representation
            window.Representation = CreateWindowRepresentation(model, context,
                windowElement.Width, windowElement.Height, 0.1); // 0.1m frame depth
            
            // Step 3: Link opening to storey and void the host wall
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(opening);
            });

            // Step 3b: IfcRelVoidsElement links the opening to its host exterior wall
            if (exteriorWallsByDirection.TryGetValue((windowElement.WallDirection, floorIndex), out var hostWall))
            {
                model.Instances.New<IfcRelVoidsElement>(r =>
                {
                    r.RelatingBuildingElement = hostWall;
                    r.RelatedOpeningElement = opening;
                });
            }
            
            // Step 4: Create IfcRelFillsElement to link window to opening
            model.Instances.New<IfcRelFillsElement>(r =>
            {
                r.GlobalId = Guid.NewGuid().ToString("N")[..22];
                r.RelatingOpeningElement = opening;
                r.RelatedBuildingElement = window;
            });
            
            // Link window to storey as well
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(window);
            });
            
            // Add window properties
            AddWindowProperties(model, window, windowElement);
        }
    }

    /// <summary>
    /// Create placement for a window element based on its position and wall direction
    /// </summary>
    private IfcAxis2Placement3D CreateWindowPlacement(IfcStore model, WindowElement windowElement, double sillHeight)
    {
        return model.Instances.New<IfcAxis2Placement3D>(a =>
        {
            // Position in storey coordinates (XY plane in IFC, Z is up)
            a.Location = model.Instances.New<IfcCartesianPoint>(p =>
                p.SetXYZ(windowElement.X, windowElement.Z, sillHeight));
            
            // Set rotation for walls on left/right (need to rotate 90 degrees)
            if (windowElement.WallDirection == WallDirection.Left || 
                windowElement.WallDirection == WallDirection.Right)
            {
                a.RefDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 1, 0));
            }
        });
    }

    /// <summary>
    /// Create box representation for the opening element
    /// </summary>
    private IfcProductDefinitionShape CreateOpeningRepresentation(IfcStore model,
        IfcGeometricRepresentationContext context, double width, double height, double depth)
    {
        // Ensure minimum depth for the opening
        depth = Math.Max(depth, 0.5);
        
        // Create extruded area solid (box for the opening void)
        var profile = model.Instances.New<IfcRectangleProfileDef>(p =>
        {
            p.ProfileType = IfcProfileTypeEnum.AREA;
            p.XDim = width;
            p.YDim = depth;
            p.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(pt => pt.SetXY(0, 0));
            });
        });
        
        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = profile;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = height;
        });
        
        var shapeRep = model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
        
        return model.Instances.New<IfcProductDefinitionShape>(pds =>
        {
            pds.Representations.Add(shapeRep);
        });
    }

    /// <summary>
    /// Create representation for the window element itself
    /// </summary>
    private IfcProductDefinitionShape CreateWindowRepresentation(IfcStore model,
        IfcGeometricRepresentationContext context, double width, double height, double frameDepth)
    {
        // Create a simple extruded profile for the window frame
        var profile = model.Instances.New<IfcRectangleProfileDef>(p =>
        {
            p.ProfileType = IfcProfileTypeEnum.AREA;
            p.XDim = width;
            p.YDim = frameDepth;
            p.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(pt => pt.SetXY(0, 0));
            });
        });
        
        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = profile;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = height;
        });
        
        var shapeRep = model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
        
        return model.Instances.New<IfcProductDefinitionShape>(pds =>
        {
            pds.Representations.Add(shapeRep);
        });
    }

    /// <summary>
    /// Add property set to window element
    /// </summary>
    private void AddWindowProperties(IfcStore model, IfcWindow window, WindowElement windowElement)
    {
        var pset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "Pset_WindowCommon";
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "IsExternal";
                p.NominalValue = new IfcBoolean(true);
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "Reference";
                p.NominalValue = new IfcIdentifier(windowElement.Id);
            }));
        });
        
        var admPset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "ADM_WindowProperties";
            ps.HasProperties.Add(CreateProperty(model, "RoomName", windowElement.RoomName));
            ps.HasProperties.Add(CreateProperty(model, "WallDirection", windowElement.WallDirection.ToString()));
            ps.HasProperties.Add(CreateProperty(model, "SillHeight", windowElement.SillHeight));
            ps.HasProperties.Add(CreateProperty(model, "OffsetAlongWall", windowElement.OffsetAlongWall));
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = pset;
            r.RelatedObjects.Add(window);
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = admPset;
            r.RelatedObjects.Add(window);
        });
    }

    /// <summary>
    /// Create doors with proper BIM relationships (Wall-Opening-Filling pattern)
    /// Similar to windows but using IfcDoor and interior wall relationships
    /// </summary>
    private void CreateDoorsWithOpenings(IfcStore model, List<IfcBuildingStorey> storeys,
        List<DoorElement> doorElements, double ceilingHeight,
        List<InteriorWallRecord> interiorWallRecords)
    {
        if (doorElements == null || doorElements.Count == 0)
            return;

        foreach (var doorElement in doorElements)
        {
            int floorIndex = doorElement.Floor;
            if (floorIndex < 0 || floorIndex >= storeys.Count) floorIndex = 0;
            var storey = storeys[floorIndex];

            var hostWall = FindHostWallForDoor(interiorWallRecords, doorElement);
            if (hostWall == null)
            {
                CreateStandaloneDoor(model, storey, doorElement);
                continue;
            }

            // 1. Create IfcOpeningElement for the door
            var opening = model.Instances.New<IfcOpeningElement>(o =>
            {
                o.Name = $"Door Opening - {doorElement.Name}";
                o.ObjectType = "OPENING";
                o.PredefinedType = IfcOpeningElementTypeEnum.OPENING;
                o.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = hostWall.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        // Door opening starts at floor level (Z = 0)
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                    });
                });
            });

            // Create opening representation (void shape)
            var openingShape = CreateDoorOpeningRepresentation(model, doorElement);
            if (openingShape != null)
            {
                opening.Representation = model.Instances.New<IfcProductDefinitionShape>(pds =>
                {
                    pds.Representations.Add(openingShape);
                });
            }

            // 2. Create IfcRelVoidsElement to link opening to wall
            model.Instances.New<IfcRelVoidsElement>(rv =>
            {
                rv.RelatingBuildingElement = hostWall;
                rv.RelatedOpeningElement = opening;
            });

            // 3. Create IfcDoor
            var door = model.Instances.New<IfcDoor>(d =>
            {
                d.Name = doorElement.Name;
                d.ObjectType = doorElement.IsExterior ? "ExteriorDoor" : "InteriorDoor";
                d.OverallHeight = doorElement.Height;
                d.OverallWidth = doorElement.Width;
                d.PredefinedType = IfcDoorTypeEnum.DOOR;
                d.OperationType = MapDoorOperationType(doorElement.OperationType);
                d.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = opening.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                    });
                });
            });

            // Create door representation
            var doorShape = CreateDoorRepresentation(model, doorElement);
            if (doorShape != null)
            {
                door.Representation = model.Instances.New<IfcProductDefinitionShape>(pds =>
                {
                    pds.Representations.Add(doorShape);
                });
            }

            // 4. Create IfcRelFillsElement to link door to opening
            model.Instances.New<IfcRelFillsElement>(rf =>
            {
                rf.RelatingOpeningElement = opening;
                rf.RelatedBuildingElement = door;
            });

            // 5. Add door to storey
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(door);
            });

            // 6. Add door properties
            AddDoorProperties(model, door, doorElement);
        }
    }

    /// <summary>
    /// Find the host wall for a door by proximity — picks the interior wall whose centre
    /// is closest to the door position, within a 2-ft tolerance.
    /// </summary>
    private IfcWall? FindHostWallForDoor(List<InteriorWallRecord> wallRecords, DoorElement door,
        double tolerance = 2.0)
    {
        return wallRecords
            .OrderBy(r => Math.Abs(r.CenterX - door.X) + Math.Abs(r.CenterZ - door.Z))
            .FirstOrDefault(r => Math.Abs(r.CenterX - door.X) + Math.Abs(r.CenterZ - door.Z) < tolerance)
            ?.Wall;
    }

    /// <summary>
    /// Create a door without wall relationship (fallback)
    /// </summary>
    private void CreateStandaloneDoor(IfcStore model, IfcBuildingStorey storey, DoorElement doorElement)
    {
        var door = model.Instances.New<IfcDoor>(d =>
        {
            d.Name = doorElement.Name;
            d.ObjectType = doorElement.IsExterior ? "ExteriorDoor" : "InteriorDoor";
            d.OverallHeight = doorElement.Height;
            d.OverallWidth = doorElement.Width;
            d.PredefinedType = IfcDoorTypeEnum.DOOR;
            d.OperationType = MapDoorOperationType(doorElement.OperationType);
            d.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
            {
                lp.PlacementRelTo = storey.ObjectPlacement;
                lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                {
                    a.Location = model.Instances.New<IfcCartesianPoint>(p => 
                        p.SetXYZ(doorElement.X, doorElement.Z, doorElement.Y));
                    if (Math.Abs(doorElement.RotationY) > 0.01)
                    {
                        a.RefDirection = model.Instances.New<IfcDirection>(d =>
                            d.SetXY(Math.Cos(doorElement.RotationY), Math.Sin(doorElement.RotationY)));
                    }
                });
            });
        });

        var doorShape = CreateDoorRepresentation(model, doorElement);
        if (doorShape != null)
        {
            door.Representation = model.Instances.New<IfcProductDefinitionShape>(pds =>
            {
                pds.Representations.Add(doorShape);
            });
        }

        model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
        {
            r.RelatingStructure = storey;
            r.RelatedElements.Add(door);
        });

        AddDoorProperties(model, door, doorElement);
    }

    /// <summary>
    /// Create door opening representation (void shape)
    /// </summary>
    private IfcShapeRepresentation? CreateDoorOpeningRepresentation(IfcStore model, DoorElement doorElement)
    {
        // Create extruded area solid for opening
        var rectangle = model.Instances.New<IfcRectangleProfileDef>(r =>
        {
            r.ProfileType = IfcProfileTypeEnum.AREA;
            r.XDim = doorElement.Width;
            r.YDim = doorElement.WallThickness + 0.1; // Slightly larger than wall
            r.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(0, 0));
            });
        });

        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = rectangle;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = doorElement.Height;
        });

        var context = model.Instances.OfType<IfcGeometricRepresentationSubContext>()
            .FirstOrDefault(c => c.ContextIdentifier == "Body") ??
            model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

        if (context == null) return null;

        return model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
    }

    /// <summary>
    /// Create door panel representation
    /// </summary>
    private IfcShapeRepresentation? CreateDoorRepresentation(IfcStore model, DoorElement doorElement)
    {
        double panelThickness = 0.1; // 0.1 feet = ~1.2 inches

        var rectangle = model.Instances.New<IfcRectangleProfileDef>(r =>
        {
            r.ProfileType = IfcProfileTypeEnum.AREA;
            r.XDim = doorElement.Width;
            r.YDim = panelThickness;
            r.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(0, 0));
            });
        });

        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = rectangle;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = doorElement.Height;
        });

        var context = model.Instances.OfType<IfcGeometricRepresentationSubContext>()
            .FirstOrDefault(c => c.ContextIdentifier == "Body") ??
            model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();

        if (context == null) return null;

        return model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
    }

    /// <summary>
    /// Map door operation type enum to IFC enum
    /// </summary>
    private IfcDoorTypeOperationEnum MapDoorOperationType(DoorOperationType opType)
    {
        return opType switch
        {
            DoorOperationType.SingleSwingLeft => IfcDoorTypeOperationEnum.SINGLE_SWING_LEFT,
            DoorOperationType.SingleSwingRight => IfcDoorTypeOperationEnum.SINGLE_SWING_RIGHT,
            DoorOperationType.DoubleDoorSingle => IfcDoorTypeOperationEnum.DOUBLE_DOOR_SINGLE_SWING,
            DoorOperationType.Sliding => IfcDoorTypeOperationEnum.SLIDING_TO_LEFT,
            DoorOperationType.Folding => IfcDoorTypeOperationEnum.FOLDING_TO_LEFT,
            _ => IfcDoorTypeOperationEnum.SINGLE_SWING_LEFT
        };
    }

    /// <summary>
    /// Add property sets to door element
    /// </summary>
    private void AddDoorProperties(IfcStore model, IfcDoor door, DoorElement doorElement)
    {
        var pset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "Pset_DoorCommon";
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "IsExternal";
                p.NominalValue = new IfcBoolean(doorElement.IsExterior);
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "Reference";
                p.NominalValue = new IfcIdentifier(doorElement.Id);
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "FireRating";
                p.NominalValue = new IfcLabel(doorElement.IsExterior ? "60min" : "20min");
            }));
        });

        var admPset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "ADM_DoorProperties";
            ps.HasProperties.Add(CreateProperty(model, "FromRoom", doorElement.FromRoomName));
            ps.HasProperties.Add(CreateProperty(model, "ToRoom", doorElement.ToRoomName));
            ps.HasProperties.Add(CreateProperty(model, "WallOrientation", doorElement.WallOrientation.ToString()));
            ps.HasProperties.Add(CreateProperty(model, "OperationType", doorElement.OperationType.ToString()));
        });

        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = pset;
            r.RelatedObjects.Add(door);
        });

        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = admPset;
            r.RelatedObjects.Add(door);
        });
    }

    /// Holds a created interior wall and its center position for door-host matching.
    private record InteriorWallRecord(IfcWall Wall, double CenterX, double CenterZ);

    private List<InteriorWallRecord> CreateInteriorWalls(IfcStore model, List<IfcBuildingStorey> storeys,
        List<GeometryData> walls, double ceilingHeight)
    {
        int wallIndex = 1;
        var records = new List<InteriorWallRecord>();

        foreach (var wallData in walls)
        {
            if (wallData.Position == null) continue;

            int floorIndex = (int)(wallData.Position.Y / ceilingHeight);
            if (floorIndex < 0 || floorIndex >= storeys.Count) floorIndex = 0;

            var storey = storeys[floorIndex];

            var wall = model.Instances.New<IfcWall>(w =>
            {
                w.Name = $"Interior Wall {wallIndex++}";
                w.ObjectType = "InteriorWall";
                w.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = storey.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        double relativeY = wallData.Position.Y - (floorIndex * ceilingHeight);
                        a.Location = model.Instances.New<IfcCartesianPoint>(p =>
                            p.SetXYZ(wallData.Position.X, wallData.Position.Z, relativeY));
                    });
                });
            });

            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(wall);
            });

            records.Add(new InteriorWallRecord(wall, wallData.Position.X, wallData.Position.Z));
        }

        return records;
    }

    private void CreateRoof(IfcStore model, IfcBuildingStorey topStorey, 
        List<RoofGeometry> roofs, HouseParameters parameters)
    {
        foreach (var roofData in roofs)
        {
            var roof = model.Instances.New<IfcRoof>(r =>
            {
                r.Name = $"{parameters.RoofType} Roof";
                r.PredefinedType = parameters.RoofType.ToLower() switch
                {
                    "gabled" => IfcRoofTypeEnum.GABLE_ROOF,
                    "flat" => IfcRoofTypeEnum.FLAT_ROOF,
                    "hip" => IfcRoofTypeEnum.HIP_ROOF,
                    _ => IfcRoofTypeEnum.USERDEFINED
                };
                r.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = topStorey.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => 
                            p.SetXYZ(0, 0, parameters.CeilingHeight));
                    });
                });
            });
            
            // Add roof properties
            var pset = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.Name = "Pset_RoofCommon";
                ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                {
                    p.Name = "Pitch";
                    p.NominalValue = new IfcPlaneAngleMeasure(parameters.RoofPitch * Math.PI / 180);
                }));
                ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                {
                    p.Name = "HasEaves";
                    p.NominalValue = new IfcBoolean(parameters.HasEaves);
                }));
                ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                {
                    p.Name = "EavesOverhang";
                    p.NominalValue = new IfcLengthMeasure(parameters.EavesOverhang);
                }));
            });
            
            model.Instances.New<IfcRelDefinesByProperties>(r =>
            {
                r.RelatingPropertyDefinition = pset;
                r.RelatedObjects.Add(roof);
            });
            
            // Link roof to storey
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = topStorey;
                r.RelatedElements.Add(roof);
            });
        }
    }

    private void AddBuildingProperties(IfcStore model, IfcBuilding building, HouseParameters parameters)
    {
        var pset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "ADM_BuildingParameters";
            ps.HasProperties.Add(CreateProperty(model, "LotSize", parameters.LotSize));
            ps.HasProperties.Add(CreateProperty(model, "Stories", parameters.Stories));
            ps.HasProperties.Add(CreateProperty(model, "CeilingHeight", parameters.CeilingHeight));
            ps.HasProperties.Add(CreateProperty(model, "FootprintWidth", parameters.FootprintWidth));
            ps.HasProperties.Add(CreateProperty(model, "FootprintDepth", parameters.FootprintDepth));
            ps.HasProperties.Add(CreateProperty(model, "BuildingShape", parameters.BuildingShape));
            ps.HasProperties.Add(CreateProperty(model, "RoofType", parameters.RoofType));
            ps.HasProperties.Add(CreateProperty(model, "ExteriorMaterial", parameters.ExteriorMaterial));
            ps.HasProperties.Add(CreateProperty(model, "WindowToWallRatio", parameters.WindowToWallRatio));
            ps.HasProperties.Add(CreateProperty(model, "RoomCount", parameters.RoomCount));
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = pset;
            r.RelatedObjects.Add(building);
        });
    }

    private void AddSpaceProperties(IfcStore model, IfcSpace space, Room room)
    {
        var pset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "ADM_RoomProperties";
            ps.HasProperties.Add(CreateProperty(model, "Floor", room.Floor));
            ps.HasProperties.Add(CreateProperty(model, "Width", room.Width));
            ps.HasProperties.Add(CreateProperty(model, "Depth", room.Depth));
            ps.HasProperties.Add(CreateProperty(model, "Area", room.Width * room.Depth));
            ps.HasProperties.Add(CreateProperty(model, "WindowCount", room.WindowCount));
            ps.HasProperties.Add(CreateProperty(model, "HasDoor", room.HasDoor));
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = pset;
            r.RelatedObjects.Add(space);
        });
    }

    private IfcPropertySingleValue CreateProperty(IfcStore model, string name, double value)
    {
        return model.Instances.New<IfcPropertySingleValue>(p =>
        {
            p.Name = name;
            p.NominalValue = new IfcReal(value);
        });
    }

    private IfcPropertySingleValue CreateProperty(IfcStore model, string name, int value)
    {
        return model.Instances.New<IfcPropertySingleValue>(p =>
        {
            p.Name = name;
            p.NominalValue = new IfcInteger(value);
        });
    }

    private IfcPropertySingleValue CreateProperty(IfcStore model, string name, string value)
    {
        return model.Instances.New<IfcPropertySingleValue>(p =>
        {
            p.Name = name;
            p.NominalValue = new IfcLabel(value);
        });
    }

    private IfcPropertySingleValue CreateProperty(IfcStore model, string name, bool value)
    {
        return model.Instances.New<IfcPropertySingleValue>(p =>
        {
            p.Name = name;
            p.NominalValue = new IfcBoolean(value);
        });
    }

    /// <summary>
    /// Create floor and ceiling slabs for each storey (IfcSlab entities)
    /// Phase 1.3: Proper BIM floor/ceiling definitions
    /// </summary>
    private void CreateFloorSlabs(IfcStore model, List<IfcBuildingStorey> storeys, HouseParameters parameters)
    {
        double slabThickness = 0.5; // 6 inches (0.5 feet)
        
        for (int i = 0; i < storeys.Count; i++)
        {
            var storey = storeys[i];
            
            // 1. Create floor slab for this storey
            var floorSlab = model.Instances.New<IfcSlab>(s =>
            {
                s.Name = $"Floor Slab - {storey.Name}";
                s.ObjectType = "FloorSlab";
                s.PredefinedType = IfcSlabTypeEnum.FLOOR;
                s.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                {
                    lp.PlacementRelTo = storey.ObjectPlacement;
                    lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                    {
                        // Floor slab sits at bottom of storey (z = -slabThickness to bring top at z=0)
                        a.Location = model.Instances.New<IfcCartesianPoint>(p => 
                            p.SetXYZ(0, 0, -slabThickness));
                    });
                });
            });
            
            // Create floor slab geometry
            var floorRepresentation = CreateSlabRepresentation(model, 
                parameters.FootprintWidth, 
                parameters.FootprintDepth, 
                slabThickness);
            floorSlab.Representation = floorRepresentation;
            
            // Link floor slab to storey
            model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
            {
                r.RelatingStructure = storey;
                r.RelatedElements.Add(floorSlab);
            });
            
            // Add floor slab properties
            AddSlabProperties(model, floorSlab, "Floor", i + 1, slabThickness);
            
            // 2. Create ceiling slab (only if not the top floor - top floor ceiling is roof)
            if (i < storeys.Count - 1)
            {
                var ceilingSlab = model.Instances.New<IfcSlab>(s =>
                {
                    s.Name = $"Ceiling Slab - {storey.Name}";
                    s.ObjectType = "CeilingSlab";
                    s.PredefinedType = IfcSlabTypeEnum.FLOOR; // Ceiling is floor of storey above
                    s.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp =>
                    {
                        lp.PlacementRelTo = storey.ObjectPlacement;
                        lp.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>(a =>
                        {
                            // Ceiling slab sits at top of storey (z = ceiling height)
                            a.Location = model.Instances.New<IfcCartesianPoint>(p => 
                                p.SetXYZ(0, 0, parameters.CeilingHeight));
                        });
                    });
                });
                
                // Create ceiling slab geometry
                var ceilingRepresentation = CreateSlabRepresentation(model, 
                    parameters.FootprintWidth, 
                    parameters.FootprintDepth, 
                    slabThickness);
                ceilingSlab.Representation = ceilingRepresentation;
                
                // Link ceiling slab to storey 
                model.Instances.New<IfcRelContainedInSpatialStructure>(r =>
                {
                    r.RelatingStructure = storey;
                    r.RelatedElements.Add(ceilingSlab);
                });
                
                // Add ceiling slab properties
                AddSlabProperties(model, ceilingSlab, "Ceiling", i + 1, slabThickness);
            }
        }
    }
    
    /// <summary>
    /// Create slab representation using extruded area solid
    /// </summary>
    private IfcProductDefinitionShape? CreateSlabRepresentation(IfcStore model, double width, double depth, double thickness)
    {
        var context = model.Instances.OfType<IfcGeometricRepresentationSubContext>()
            .FirstOrDefault(c => c.ContextIdentifier == "Body") ??
            model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
        
        if (context == null) return null;
        
        // Create rectangular profile for slab
        var profile = model.Instances.New<IfcRectangleProfileDef>(p =>
        {
            p.ProfileType = IfcProfileTypeEnum.AREA;
            p.XDim = width;
            p.YDim = depth;
            p.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                // Center the profile
                a.Location = model.Instances.New<IfcCartesianPoint>(pt => 
                    pt.SetXY(width / 2, depth / 2));
            });
        });
        
        // Extrude upward to create slab solid
        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = profile;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = thickness;
        });
        
        var shapeRep = model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
        
        return model.Instances.New<IfcProductDefinitionShape>(pds =>
        {
            pds.Representations.Add(shapeRep);
        });
    }
    
    /// <summary>
    /// Add property sets to slab elements
    /// </summary>
    private void AddSlabProperties(IfcStore model, IfcSlab slab, string slabType, int floor, double thickness)
    {
        var pset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "Pset_SlabCommon";
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "IsExternal";
                p.NominalValue = new IfcBoolean(false);
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "LoadBearing";
                p.NominalValue = new IfcBoolean(true);
            }));
            ps.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
            {
                p.Name = "Reference";
                p.NominalValue = new IfcIdentifier($"{slabType}-F{floor}");
            }));
        });
        
        var admPset = model.Instances.New<IfcPropertySet>(ps =>
        {
            ps.Name = "ADM_SlabProperties";
            ps.HasProperties.Add(CreateProperty(model, "SlabType", slabType));
            ps.HasProperties.Add(CreateProperty(model, "Floor", (double)floor));
            ps.HasProperties.Add(CreateProperty(model, "Thickness", thickness));
            ps.HasProperties.Add(CreateProperty(model, "Material", "Concrete"));
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = pset;
            r.RelatedObjects.Add(slab);
        });
        
        model.Instances.New<IfcRelDefinesByProperties>(r =>
        {
            r.RelatingPropertyDefinition = admPset;
            r.RelatedObjects.Add(slab);
        });
    }

    private IfcProductDefinitionShape CreateBoxRepresentation(IfcStore model, double width, double depth, double height)
    {
        // Get the 3D context
        var context = model.Instances.OfType<IfcGeometricRepresentationContext>()
            .FirstOrDefault(c => c.ContextType == "Model");
        
        if (context == null) return null!;
        
        // Create extruded area solid (box)
        var profile = model.Instances.New<IfcRectangleProfileDef>(p =>
        {
            p.ProfileType = IfcProfileTypeEnum.AREA;
            p.XDim = width;
            p.YDim = depth;
            p.Position = model.Instances.New<IfcAxis2Placement2D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(pt => pt.SetXY(0, 0));
            });
        });
        
        var solid = model.Instances.New<IfcExtrudedAreaSolid>(s =>
        {
            s.SweptArea = profile;
            s.Position = model.Instances.New<IfcAxis2Placement3D>(a =>
            {
                a.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
            });
            s.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
            s.Depth = height;
        });
        
        var shapeRep = model.Instances.New<IfcShapeRepresentation>(sr =>
        {
            sr.ContextOfItems = context;
            sr.RepresentationIdentifier = "Body";
            sr.RepresentationType = "SweptSolid";
            sr.Items.Add(solid);
        });
        
        return model.Instances.New<IfcProductDefinitionShape>(pds =>
        {
            pds.Representations.Add(shapeRep);
        });
    }
}
