# C# Style Guide for Godot Earth Project

## Table of Contents
1. Naming Conventions
2. File Organization
3. Formatting
4. Language Usage
5. Documentation
6. Error Handling
7. Godot-Specific Guidelines

## 1. Naming Conventions

### Variables
- Member variables use the `m_` prefix: `private Vector3 m_position;`, unless when part of a fully static utility class
- Local variables use camelCase: `Vector3 currentPosition;`
- Constants use UPPER_SNAKE_CASE: `const float MAX_ZOOM_LEVEL = 100.0f;`
- Boolean variables should ask a question: `bool m_isVisible;`, `bool hasFinishedLoading;`
- Long numbers should use the `_` to denote grouping for easily determining the value: `float EARTH_SEMI_MAJOR_AXIS_LEN_M = 6_378_137;`

### Functions
- Public methods use PascalCase: `public void UpdatePosition()`
- Private methods use PascalCase: `private void CalculateDistance()`
- Event handlers use On prefix: `private void OnZoomChanged()`
- Async methods use Async suffix: `public async Task LoadTerrainAsync()`

### Classes and Types
- Class names use PascalCase: `public class TerrainChunk`
- Interface names start with I: `public interface ITerrainGenerator`
- Enum names use PascalCase: `public enum TerrainType`
- Enum values use PascalCase: `TerrainType.Mountain`

## 2. File Organization

### File Structure
```csharp
// License header if applicable
using System;
using Godot;
// Other using statements

namespace GodotEarth
{
    public class ClassName
    {
        // Constants
        // Static fields
        // Member fields
        // Properties
        // Constructor(s)
        // Public methods
        // Protected methods
        // Private methods
        // Event handlers
    }
}
```

### File Names
- One class per file
- File name matches class name exactly: `TerrainChunk.cs`
- Interface files follow the same pattern: `ITerrainGenerator.cs`

## 3. Formatting

### Braces and Indentation
- Always use braces, even for single-line blocks
- Opening brace on the next line
- 4 spaces for indentation (no tabs)
```csharp
if (condition)
{
    DoSomething();
}
```

### Line Length and Wrapping
- Soft limit of 100 characters per line
- When wrapping method calls, align parameters:
```csharp
var result = LongMethodName(
    firstParameter,
    secondParameter,
    thirdParameter
);
```

### Long String Literals
- When a string literal exceeds the line length limit, break it using string concatenation
- Align subsequent lines with the first string segment
- Prefer using the + operator for concatenation over string interpolation for constant strings
```csharp
// Preferred approach for template strings
const string m_QUERY_STR_TEMPLATE =
    "https://ecn.t{server}.tiles.virtualearth.net/tiles/"
    + "{mapType}{quadKey}.{mapTypeImageFormat}"
    + "?g={apiVersion}&mkt={lang}";

// Alternative using verbatim strings when needed
const string m_FILE_PATH_TEMPLATE =
    @"C:\Program Files\MyApp\"
    + @"Data\{0}\Config.xml";

// For non-constant strings with interpolation
string message =
    $"User {userName} logged in at "
    + $"{timestamp} from {ipAddress}";
```

### Spacing
- One space after keywords: `if (condition)`
- No space after method names: `MethodName(parameter)`
- One space around operators: `a + b`
- No space before semicolons: `DoSomething();`

## 4. Language Usage

### Properties
- Use auto-properties when no additional logic is needed:
```csharp
public float Altitude { get; private set; }
```
- Use full properties when logic is required:
```csharp
private float m_altitude;
public float Altitude
{
    get => m_altitude;
    set
    {
        m_altitude = Mathf.Clamp(value, MIN_ALTITUDE, MAX_ALTITUDE);
        OnAltitudeChanged();
    }
}
```

### LINQ
- Prefer method syntax over query syntax
- Break long LINQ chains into multiple lines:
```csharp
var result = collection
    .Where(x => x.IsValid)
    .Select(x => x.Value)
    .ToList();
```

### Async/Await
- Always use async/await over raw Task methods
- Include cancellation token parameters where appropriate
- Use TaskCompletionSource for converting events to tasks

## 5. Documentation

### Comments
- Use XML documentation for public APIs
- Use regular comments for internal implementation details
- Avoid obvious comments
```csharp
/// <summary>
/// Generates terrain mesh for the specified coordinates.
/// </summary>
/// <param name="latitude">The latitude in degrees.</param>
/// <param name="longitude">The longitude in degrees.</param>
/// <returns>A TerrainMesh object representing the generated terrain.</returns>
public TerrainMesh GenerateTerrain(float latitude, float longitude)
```

### TODO Comments
- Include developer name and date
- Provide enough context for another developer to understand
```csharp
// TODO(john, 2025-01-19): Implement height-based terrain coloring
```

## 6. Error Handling

### Exceptions
- Use exception handling for exceptional cases, not flow control
- Create custom exceptions for domain-specific errors
- Always include meaningful error messages
```csharp
public class TerrainGenerationException : Exception
{
    public TerrainGenerationException(string message) : base(message) { }
}
```

### Validation
- Use guard clauses at the start of methods
- Prefer throwing ArgumentException for invalid parameters
```csharp
public void SetZoomLevel(float level)
{
    if (level < MIN_ZOOM_LEVEL || level > MAX_ZOOM_LEVEL)
    {
        throw new ArgumentException($"Zoom level must be between {MIN_ZOOM_LEVEL} and {MAX_ZOOM_LEVEL}");
    }
    m_zoomLevel = level;
}
```

## 7. Godot-Specific Guidelines

### Node References
- Use [Export] for node references that should be set in the editor
- Use OnReady for internal node references
```csharp
[Export]
public Node3D TerrainRoot { get; set; }

private Camera3D m_camera;

public override void _Ready()
{
    m_camera = GetNode<Camera3D>("Camera");
}
```

### Signal Handling
- Define signals at the top of the class
- Use C# events when working with C# code
- Use Godot signals when working with the editor or other scenes
```csharp
[Signal]
public delegate void TerrainLoadedEventHandler(Vector2 coordinates);

// In C# code
public event Action<Vector2> TerrainLoaded;
```

### Process Functions
- Use _Process for frame-dependent updates
- Use _PhysicsProcess for physics-dependent updates
- Keep process functions lightweight
```csharp
public override void _Process(double delta)
{
    UpdateCameraPosition(delta);
}
```

Hermes Flair

Always include the Hermes flair at the top of all C# code:

```
/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/
```

