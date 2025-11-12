# Architectural Dream Machine - Backend

ASP.NET Core Web API for generating architectural designs from natural language prompts.

## Quick Start

```bash
dotnet restore
dotnet run
```

API will be available at `http://localhost:5162` (check console for actual port)

## Running Tests

```bash
dotnet test
```

## API Documentation

Once running, visit `http://localhost:5162/swagger` for interactive API documentation.

## Configuration

### Database

Default: SQLite (`architecturaldreammachine.db`)

To use SQL Server, set environment variable:
```bash
export SQL_SERVER_CONNECTION_STRING="Server=localhost;Database=ArchitecturalDreamMachine;..."
```

### CORS

Configured to allow all origins for development. Update `Program.cs` for production.

## Project Structure

```
ArchitecturalDreamMachineBackend/
├── Controllers/
│   └── DesignsController.cs       # API endpoints
├── Data/
│   ├── AppDbContext.cs            # EF Core context
│   ├── Design.cs                  # Design entity
│   └── StyleTemplate.cs           # Style template entity
├── Geometry/
│   ├── HouseParameters.cs         # House generation parameters
│   ├── Material.cs                # Material properties
│   ├── Mesh.cs                    # 3D mesh structure
│   └── Vector3.cs                 # 3D vector
├── Tests/
│   ├── DesignsControllerTests.cs  # Controller tests
│   ├── PromptParserTests.cs       # Parser tests
│   └── GeometryTests.cs           # Geometry tests
├── PromptParser.cs                # Prompt parsing logic
└── Program.cs                     # Application entry point
```

## Dependencies

- Microsoft.EntityFrameworkCore.Sqlite 8.0.0
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0
- Microsoft.EntityFrameworkCore.Tools 8.0.0
- xUnit 2.9.3
- Moq 4.20.72

## Notes

- Database is auto-created with seeded style templates on first run
- Uses in-memory database for tests
- Supports keyword-based style matching (extendable to ML/NLP)
