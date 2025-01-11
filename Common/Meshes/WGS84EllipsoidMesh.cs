using Godot;
using System;
using System.Collections.Generic;

public partial class WGS84EllipsoidMesh : MeshInstance3D
{
	private const int LATITUDE_SEGMENTS = 32;
	private const int LONGITUDE_SEGMENTS = 64;

	[Export]
	private bool showWireframe = false;
	private bool previousWireframeState = false;

	public override void _Ready()
	{
		UpdateWireframeState();
		CreateEllipsoidMesh();
	}

	public override void _Process(double delta)
	{
		if (showWireframe != previousWireframeState)
		{
			UpdateWireframeState();
		}
	}

	private void CreateEllipsoidMesh()
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();

		// Generate vertices for each point on our grid
		for (int lat = 0; lat <= LATITUDE_SEGMENTS; lat++)
		{
			// Convert latitude segment to angle in radians
			// Range from -π/2 to π/2 (South pole to North pole)
			double phi = Math.PI * ((double)lat / LATITUDE_SEGMENTS - 0.5);
			double sinPhi = Math.Sin(phi);
			double cosPhi = Math.Cos(phi);

			for (int lon = 0; lon <= LONGITUDE_SEGMENTS; lon++)
			{
				// Convert longitude segment to angle in radians
				// Range from 0 to 2π (complete circle)
				double lambda = 2 * Math.PI * (double)lon / LONGITUDE_SEGMENTS;
				double sinLambda = Math.Sin(lambda);
				double cosLambda = Math.Cos(lambda);

				// Calculate position on ellipsoid surface
				double x = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * cosPhi * cosLambda;
				double y = SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * sinPhi;  // Using Y as up-axis in Godot
				double z = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * cosPhi * sinLambda;

				// Add vertex
				vertices.Add(new Vector3((float)x, (float)y, (float)z));

				// Calculate normal - for an ellipsoid, normals aren't simply normalized position vectors
				// For an ellipsoid, we want normals pointing outward
				// The normal at any point is proportional to the gradient of the ellipsoid equation
				// (x²/a² + y²/b² + z²/a² = 1)
				double nx = x / (SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);
				double ny = y / (SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM);
				double nz = z / (SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);
				double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);

				normals.Add(new Vector3(
					(float)(nx / length),
					(float)(ny / length),
					(float)(nz / length)
				));

				// Calculate UV coordinates
				// U ranges from 0 to 1 (longitude)
				// V ranges from 0 to 1 (latitude)
				uvs.Add(new Vector2(
					(float)lon / LONGITUDE_SEGMENTS,
					(float)lat / LATITUDE_SEGMENTS
				));

				// Generate indices for triangles
				if (lat < LATITUDE_SEGMENTS && lon < LONGITUDE_SEGMENTS)
				{
					int current = lat * (LONGITUDE_SEGMENTS + 1) + lon;
					int next = current + LONGITUDE_SEGMENTS + 1;

					// First triangle
					indices.Add(current);
					indices.Add(current + 1);
					indices.Add(next);

					// Second triangle
					indices.Add(current + 1);
					indices.Add(next + 1);
					indices.Add(next);

				}
			}
		}

		// Assign arrays
		surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		// Create the mesh
		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		Mesh = arrayMesh;
	}

	private void UpdateWireframeState()
	{
		RenderingServer.SetDebugGenerateWireframes(showWireframe);
		GetViewport().SetDebugDraw(showWireframe ?
			Viewport.DebugDrawEnum.Wireframe :
			Viewport.DebugDrawEnum.Disabled);
		previousWireframeState = showWireframe;
	}


}
