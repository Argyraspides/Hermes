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
using System.Linq;

/// <summary>
/// This is a mesh generator that contains function which generate meshes specifically for the WGS84 ellipsoid standard. Used for
/// constructing the surface of the Earth as a series of meshes
///
/// To learn more about what WGS84 means, see below:
///
/// https://en.wikipedia.org/wiki/World_Geodetic_System
/// </summary>
public static class WGS84EllipsoidMeshGenerator
{
    private const int LATITUDE_SEGMENTS = 32;
    private const int LONGITUDE_SEGMENTS = 32;

    /// <summary>
    /// Returns a MeshInstance3D quadrilateral or triangular segment that corresponds
    /// to a particular latitude and longitude (center of the segment) and a latitude
    /// and longitude range that the mesh should cover.
    ///
    /// The returned mesh segment is curved and represents the surface of a WGS84
    /// ellipsoid. Units are in kilometers.
    ///
    /// For most latitude/longitude combinations, this function returns a
    /// quadrilateral mesh made of two triangles. However, when the segment includes
    /// either the north pole (π/2°) or south pole (-π/2°), it returns a single triangle
    /// instead. This special handling for poles prevents overlapping geometry when
    /// multiple segments are combined to create a complete ellipsoid mesh, since all
    /// points at a pole share the same location regardless of longitude.
    /// </summary>
    /// <param name="lat">The center latitude of the segment in radians</param>
    /// <param name="lon">The center longitude of the segment in radians</param>
    /// <param name="latRange">The total latitude range (height) the segment should cover in radians</param>
    /// <param name="lonRange">The total longitude range (width) the segment should cover in radians</param>
    /// <returns>
    /// An ArrayMesh containing either a quadrilateral (for non‐pole segments) or a
    /// triangle (for pole segments).
    /// </returns>
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

        float semiMajorAxisUnit = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM /
                                  SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM;

        float semiMinorAxisUnit = 1;

        //=== CASE 1: North Pole segment (triangle)
        if (touchesNorthPole)
        {
            // 3 vertices: bottom-left, bottom-right, top (pole)
            float[] x =
            {
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon - halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon + halfLonRange), 0 // Pole
            };

            float[] y =
            {
                semiMinorAxisUnit * Mathf.Sin(lat - halfLatRange), 1 * Mathf.Sin(lat - halfLatRange),
                1 // North pole is + (semi-minor)
            };

            float[] z =
            {
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon - halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon + halfLonRange), 0 // Pole
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
            float[] x =
            {
                0, // Pole
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon + halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon - halfLonRange),
            };

            float[] y =
            {
                -semiMinorAxisUnit, // South pole
                semiMinorAxisUnit * Mathf.Sin(lat + halfLatRange), semiMinorAxisUnit * Mathf.Sin(lat + halfLatRange)
            };

            float[] z =
            {
                0, // Pole
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon + halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon - halfLonRange),
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
            float[] x =
            {
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon - halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Cos(lon + halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon + halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Cos(lon - halfLonRange)
            };

            float[] y =
            {
                semiMinorAxisUnit * Mathf.Sin(lat - halfLatRange),
                semiMinorAxisUnit * Mathf.Sin(lat - halfLatRange),
                semiMinorAxisUnit * Mathf.Sin(lat + halfLatRange), semiMinorAxisUnit * Mathf.Sin(lat + halfLatRange)
            };

            float[] z =
            {
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon - halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat - halfLatRange) * Mathf.Sin(lon + halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon + halfLonRange),
                semiMajorAxisUnit * Mathf.Cos(lat + halfLatRange) * Mathf.Sin(lon - halfLonRange)
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

        // Offset the mesh so that the geometric center of the mesh lies in the origin. This is so if we ever
        // scale the mesh, we are scaling it along its own plane and not moving it in 3D space.
        Vector3 center = vertices.Aggregate(
            Vector3.Zero,
            (acc, vec) => acc + vec
        ) / vertices.Count;

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] -= center;
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
                double y = SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM * sinPhi; // Using Y as up-axis in Godot
                double z = SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM * cosPhi * sinLambda;

                // Add vertex
                vertices.Add(new Vector3((float)x, (float)y, (float)z));

                // Calculate normal - for an ellipsoid, normals aren't simply normalized position vectors
                // For an ellipsoid, we want normals pointing outward
                // The normal at any point is proportional to the gradient of the ellipsoid equation
                // (x²/a² + y²/b² + z²/a² = 1)
                double nx = x / (SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM *
                                 SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);
                double ny = y / (SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM *
                                 SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM);
                double nz = z / (SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM *
                                 SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);
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
        return arrayMesh;
    }
}
