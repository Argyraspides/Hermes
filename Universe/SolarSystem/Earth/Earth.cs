using Godot;
using System;

public partial class Earth : StaticBody3D
{
	float semiMajorAxisLen = 6378.1370f;
	float semiMinorAxisLen = 6356.752314245f;
	SphereMesh earthMesh;

	private const int BASE_RINGS = 2;
	private const int BASE_SEGMENTS = 4;
	private const float MAX_SUBDIVISIONS = 8f;

	private Camera3D camera;
	private MeshInstance3D earthMeshInstance;

	public override void _Ready()
	{
		InitializeEarth();
		camera = GetNode<Camera3D>("../Camera3D");
	}


	private ShaderMaterial gridMaterial;

	private void InitializeEarth()
	{
		MeshInstance3D earthMeshInstance = GetNode<MeshInstance3D>("EarthMesh");
		earthMesh = (SphereMesh)earthMeshInstance.Mesh;
		earthMesh.Radius = semiMajorAxisLen;
		earthMesh.Height = semiMinorAxisLen * 2.0f;

		// Set initial mesh resolution
		earthMesh.Rings = BASE_RINGS;
		earthMesh.RadialSegments = BASE_SEGMENTS;

		// Create and apply the grid material
		gridMaterial = new ShaderMaterial();
		gridMaterial.Shader = GD.Load<Shader>("res://earth_grid.gdshader"); 
		// Apply material to the mesh instance
		earthMeshInstance.MaterialOverride = gridMaterial;
	}

	private void UpdateMeshDetail()
	{
		if (camera == null) return;

		float distance = camera.GlobalPosition.Length();
		float normalizedDist = (distance - semiMajorAxisLen) / (semiMajorAxisLen * 4);
		normalizedDist = Mathf.Clamp(normalizedDist, 0f, 1f);

		float subdivisionMultiplier = Mathf.Lerp(MAX_SUBDIVISIONS, 1f, normalizedDist);

		int newRings = (int)(BASE_RINGS * subdivisionMultiplier);
		int newSegments = (int)(BASE_SEGMENTS * subdivisionMultiplier);

		if (Mathf.Abs(earthMesh.Rings - newRings) > 2)
		{
			earthMesh.Rings = newRings;
			earthMesh.RadialSegments = newSegments;

			if (gridMaterial != null)
			{
				gridMaterial.SetShaderParameter("subdivision_amount", new Vector2(newSegments, newRings));
				GD.Print($"Updated subdivisions: Segments={newSegments}, Rings={newRings}");
			}
		}
	}


	public override void _Process(double delta)
	{
		UpdateMeshDetail();
	}
}