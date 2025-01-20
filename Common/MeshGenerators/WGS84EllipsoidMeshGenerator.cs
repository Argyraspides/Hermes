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


using Godot;
using System;
using System.Collections.Generic;

// TODO: Add documentation on what this is
public static class WGS84EllipsoidMeshGenerator
{
	private const int m_LATITUDE_SEGMENTS = 32;
	private const int m_LONGITUDE_SEGMENTS = 32;
	/**
	  Returns a MeshInstance3D quadrilateral or triangular segment that corresponds
	  to a particular latitude and longitude (center of the segment) and a latitude
	  and longitude range that the mesh should cover.

	  The returned mesh segment is curved and represents the surface of a WGS84
	  ellipsoid. Units are in kilometers.

	  For most latitude/longitude combinations, this function returns a
	  quadrilateral mesh made of two triangles. However, when the segment includes
	  either the north pole (90°) or south pole (-90°), it returns a single triangle
	  instead. This special handling for poles prevents overlapping geometry when
	  multiple segments are combined to create a complete ellipsoid mesh, since all
	  points at a pole share the same location regardless of longitude.

	  Parameters:
		lat:       The center latitude of the segment in radians
		lon:       The center longitude of the segment in radians
		latRange:  The total latitude range (height) the segment should cover in radians
		lonRange:  The total longitude range (width) the segment should cover in radians

	  Returns:
		An ArrayMesh containing either a quadrilateral (for non‐pole segments) or a
		triangle (for pole segments).
	*/
	// TODO: This function is very very long (not necessarily a bad thing). Can definitely be cleaned up other
	// wise if not in terms of length.
	public static ArrayMesh CreateEllipsoidMeshSegment(float lat, float lon, float latRange, float lonRange)
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		// UV2 stores lat/lon for your reprojection shader:
		var uv2s = new List<Vector2>();
		var indices = new List<int>();

		// Half-ranges:
		float halfLatRange = latRange / 2.0f;
		float halfLonRange = lonRange / 2.0f;

		// Check whether this segment touches the north or south pole
		bool touchesNorthPole = (lat + halfLatRange) >= (Mathf.Pi / 2.0f);
		bool touchesSouthPole = (lat - halfLatRange) <= (-Mathf.Pi / 2.0f);

		//=== CASE 1: North Pole segment (triangle)
		if (touchesNorthPole)
		{
			// 3 vertices: bottom-left, bottom-right, top (pole)
			float[] x = {
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon - halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon + halfLonRange),
			0 // Pole
        };

			float[] y = {
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat - halfLatRange),
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat - halfLatRange),
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM // North pole is + (semi-minor)
        };

			float[] z = {
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon - halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon + halfLonRange),
			0 // Pole
        };

			for (int i = 0; i < 3; i++)
			{
				Vector3 vtx = new Vector3(x[i], y[i], z[i]);
				vertices.Add(vtx);
				normals.Add(vtx.Normalized());

				// Basic UV
				float u = 0.0f, v = 0.0f;
				// Lat/lon in UV2:
				float u2 = 0.0f, v2 = 0.0f;

				if (i == 0) // bottom-left
				{
					u = 0.0f;
					v = 1.0f;
					u2 = lon - halfLonRange;
					v2 = lat - halfLatRange;
				}
				else if (i == 1) // bottom-right
				{
					u = 1.0f;
					v = 1.0f;
					u2 = lon + halfLonRange;
					v2 = lat - halfLatRange;
				}
				else // i == 2 => pole (top)
				{
					u = 0.5f;
					v = 0.0f;
					u2 = lon;
					v2 = lat + halfLatRange;
				}

				uvs.Add(new Vector2(u, v));
				uv2s.Add(new Vector2(u2, v2));
			}

			// One triangle: [0, 1, 2]
			indices.Add(0);
			indices.Add(1);
			indices.Add(2);
		}
		//=== CASE 2: South Pole segment (triangle)
		else if (touchesSouthPole)
		{
			// 3 vertices: bottom (pole), top-right, top-left
			float[] x = {
			0, // Pole
            SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon + halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon - halfLonRange),
		};

			float[] y = {
			-SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM, // South pole
            SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat + halfLatRange),
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat + halfLatRange),
		};

			float[] z = {
			0, // Pole
            SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon + halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon - halfLonRange),
		};

			for (int i = 0; i < 3; i++)
			{
				Vector3 vtx = new Vector3(x[i], y[i], z[i]);
				vertices.Add(vtx);
				normals.Add(vtx.Normalized());

				float u = 0.0f, v = 0.0f;
				float u2 = 0.0f, v2 = 0.0f;

				if (i == 0) // bottom => south pole
				{
					u = 0.5f;
					v = 1.0f;
					u2 = lon;
					v2 = lat - halfLatRange;
				}
				else if (i == 1) // top-right
				{
					u = 1.0f;
					v = 0.0f;
					u2 = lon + halfLonRange;
					v2 = lat + halfLatRange;
				}
				else // i == 2 => top-left
				{
					u = 0.0f;
					v = 0.0f;
					u2 = lon - halfLonRange;
					v2 = lat + halfLatRange;
				}

				uvs.Add(new Vector2(u, v));
				uv2s.Add(new Vector2(u2, v2));
			}

			// One triangle: [0, 1, 2]
			indices.Add(0);
			indices.Add(1);
			indices.Add(2);
		}
		//=== CASE 3: Regular quadrilateral (split into two triangles)
		else
		{
			// 4 corners: bottom-left, bottom-right, top-right, top-left
			float[] x = {
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon - halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon + halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon + halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon - halfLonRange)
		};

			float[] y = {
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat - halfLatRange),
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat - halfLatRange),
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat + halfLatRange),
			SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * Mathf.Sin(lat + halfLatRange)
		};

			float[] z = {
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon - halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon + halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon + halfLonRange),
			SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon - halfLonRange)
		};

			// Add the four corners in CCW order
			for (int i = 0; i < 4; i++)
			{
				Vector3 vtx = new Vector3(x[i], y[i], z[i]);
				vertices.Add(vtx);
				normals.Add(vtx.Normalized());

				float u = 0.0f, v = 0.0f;
				float u2 = 0.0f, v2 = 0.0f;

				if (i == 0) // bottom-left
				{
					u = 0.0f;
					v = 1.0f;
					u2 = lon - halfLonRange;
					v2 = lat - halfLatRange;
				}
				else if (i == 1) // bottom-right
				{
					u = 1.0f;
					v = 1.0f;
					u2 = lon + halfLonRange;
					v2 = lat - halfLatRange;
				}
				else if (i == 2) // top-right
				{
					u = 1.0f;
					v = 0.0f;
					u2 = lon + halfLonRange;
					v2 = lat + halfLatRange;
				}
				else if (i == 3) // top-left
				{
					u = 0.0f;
					v = 0.0f;
					u2 = lon - halfLonRange;
					v2 = lat + halfLatRange;
				}

				uvs.Add(new Vector2(u, v));
				uv2s.Add(new Vector2(u2, v2));
			}

			// Two triangles for the quad:
			//  (0,1,2) and (0,2,3)
			indices.Add(0);
			indices.Add(1);
			indices.Add(2);

			indices.Add(0);
			indices.Add(2);
			indices.Add(3);
		}

		surfaceArray[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV2] = uv2s.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		return arrayMesh;
	}

	public static ArrayMesh CreateFullEllipsoidMesh()
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();
		var indices = new List<int>();

		// Generate vertices for each point on our grid
		for (int lat = 0; lat <= m_LATITUDE_SEGMENTS; lat++)
		{
			// Convert latitude segment to angle in radians
			// Range from -π/2 to π/2 (South pole to North pole)
			double phi = Math.PI * ((double)lat / m_LATITUDE_SEGMENTS - 0.5);
			double sinPhi = Math.Sin(phi);
			double cosPhi = Math.Cos(phi);

			for (int lon = 0; lon <= m_LONGITUDE_SEGMENTS; lon++)
			{
				// Convert longitude segment to angle in radians
				// Range from 0 to 2π (complete circle)
				double lambda = 2 * Math.PI * (double)lon / m_LONGITUDE_SEGMENTS;
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
					(float)lon / m_LONGITUDE_SEGMENTS,
					(float)lat / m_LATITUDE_SEGMENTS
				));

				// Generate indices for triangles
				if (lat < m_LATITUDE_SEGMENTS && lon < m_LONGITUDE_SEGMENTS)
				{
					int current = lat * (m_LONGITUDE_SEGMENTS + 1) + lon;
					int next = current + m_LONGITUDE_SEGMENTS + 1;

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
		return arrayMesh;
	}

}
