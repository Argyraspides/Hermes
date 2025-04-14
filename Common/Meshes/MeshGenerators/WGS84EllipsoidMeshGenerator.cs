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

using Hermes.Common.Map.Utils;

namespace Hermes.Common.Meshes.MeshGenerators;

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

        /// <summary>
        /// Creates an ellipsoid mesh segment for the WGS84 ellipsoid. If the mesh touches the poles, the returned
        /// mesh will be a triangle, otherwise a quadrilateral composed of two triangles.
        ///
        /// The returned mesh will be normalized. In Hermes, the convention is that null island lies on the +ve
        /// z-axis. The z-axis will therefore pierce the equator, which is the furthest point away from the Earth's center.
        /// The returned mesh is normalized, meaning that all values are taken as the fraction of the semi-major axis, since
        /// this is the furthest we can be from Earth's center.
        ///
        /// The returned mesh will have its geometric center lie in the origin, so any scaled transformations will be done
        /// from the center.
        /// </summary>
        /// <param name="lat">The center latitude of the mesh</param>
        /// <param name="lon">The center longitude of the mesh</param>
        /// <param name="latRange">The range of latitude the mesh is meant to cover</param>
        /// <param name="lonRange">The range of longitude the mesh is meant to cover</param>
        /// <returns>A triangle ArrayMesh if the mesh touches the poles, otherwise a quadrilateral mesh</returns>
        public static ArrayMesh CreateEllipsoidMeshSegment(float lat, float lon, float latRange, float lonRange)
        {
            var surfaceArray = new Godot.Collections.Array();
            surfaceArray.Resize((int)Mesh.ArrayType.Max);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            // UV2 stores lat/lon for the reprojection shader:
            var uv2s = new List<Vector2>();
            var indices = new List<int>();

            // Half-ranges:
            float halfLatRange = latRange / 2.0f;
            float halfLonRange = lonRange / 2.0f;

            // Check whether this segment touches the north or south pole
            bool touchesNorthPole = (lat + halfLatRange) >= (Mathf.Pi / 2.0f);
            bool touchesSouthPole = (lat - halfLatRange) <= (-Mathf.Pi / 2.0f);

            // Calculate the minor-to-major axis ratio
            float minorToMajorRatio = (float)(SolarSystemConstants.EARTH_SEMI_MINOR_AXIS_LEN_KM /
                                              SolarSystemConstants.EARTH_SEMI_MAJOR_AXIS_LEN_KM);

            //=== CASE 1: North Pole segment (triangle)
            if (touchesNorthPole)
            {
                // 3 vertices: bottom-left, bottom-right, top (pole)
                float[] x =
                {
                    (float)MapUtils.LatLonToCartesianWGS84X(lat - halfLatRange, lon - halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84X(lat - halfLatRange, lon + halfLonRange),
                    0 // Pole
                };

                float[] y =
                {
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat - halfLatRange),
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat - halfLatRange),
                    minorToMajorRatio // North pole, normalized
                };

                float[] z =
                {
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat - halfLatRange, lon - halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat - halfLatRange, lon + halfLonRange),
                    0 // Pole
                };

                for (int i = 0; i < 3; i++)
                {
                    Vector3 vtx = new Vector3(x[i], y[i], z[i]);
                    vertices.Add(vtx);
                    normals.Add(vtx.Normalized());

                    // Lat/lon in UV2:
                    float u2 = 0.0f, v2 = 0.0f;

                    if (i == 0) // bottom-left
                    {
                        u2 = lon - halfLonRange;
                        v2 = lat - halfLatRange;
                    }
                    else if (i == 1) // bottom-right
                    {
                        u2 = lon + halfLonRange;
                        v2 = lat - halfLatRange;
                    }
                    else // i == 2 => pole (top)
                    {
                        u2 = lon;
                        v2 = lat + halfLatRange;
                    }

                    uv2s.Add(new Vector2(u2, v2));
                }

                // One triangle with clockwise winding: [0, 2, 1]
                indices.Add(0);
                indices.Add(2);
                indices.Add(1);
            }
            //=== CASE 2: South Pole segment (triangle)
            else if (touchesSouthPole)
            {
                // 3 vertices: bottom (pole), top-right, top-left
                float[] x =
                {
                    0, // Pole
                    (float)MapUtils.LatLonToCartesianWGS84X(lat + halfLatRange, lon + halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84X(lat + halfLatRange, lon - halfLonRange),
                };

                float[] y =
                {
                    -minorToMajorRatio, // South pole, normalized
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat + halfLatRange),
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat + halfLatRange)
                };

                float[] z =
                {
                    0, // Pole
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat + halfLatRange, lon + halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat + halfLatRange, lon - halfLonRange),
                };

                for (int i = 0; i < 3; i++)
                {
                    Vector3 vtx = new Vector3(x[i], y[i], z[i]);
                    vertices.Add(vtx);
                    normals.Add(vtx.Normalized());

                    float u2 = 0.0f, v2 = 0.0f;

                    if (i == 0) // bottom => south pole
                    {
                        u2 = lon;
                        v2 = lat - halfLatRange;
                    }
                    else if (i == 1) // top-right
                    {
                        u2 = lon + halfLonRange;
                        v2 = lat + halfLatRange;
                    }
                    else // i == 2 => top-left
                    {
                        u2 = lon - halfLonRange;
                        v2 = lat + halfLatRange;
                    }

                    uv2s.Add(new Vector2(u2, v2));
                }

                // One triangle with clockwise winding: [0, 2, 1]
                indices.Add(0);
                indices.Add(2);
                indices.Add(1);
            }
            //=== CASE 3: Regular quadrilateral (split into two triangles)
            else
            {
                // 4 corners: bottom-left, bottom-right, top-right, top-left
                float[] x =
                {
                    (float)MapUtils.LatLonToCartesianWGS84X(lat - halfLatRange, lon - halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84X(lat - halfLatRange, lon + halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84X(lat + halfLatRange, lon + halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84X(lat + halfLatRange, lon - halfLonRange)
                };

                float[] y =
                {
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat - halfLatRange),
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat - halfLatRange),
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat + halfLatRange),
                    (float)MapUtils.LatLonToCartesianWGS84Y(lat + halfLatRange)
                };

                float[] z =
                {
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat - halfLatRange, lon - halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat - halfLatRange, lon + halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat + halfLatRange, lon + halfLonRange),
                    (float)MapUtils.LatLonToCartesianWGS84Z(lat + halfLatRange, lon - halfLonRange)
                };

                // Add the four corners in the same order as before
                for (int i = 0; i < 4; i++)
                {
                    Vector3 vtx = new Vector3(x[i], y[i], z[i]);
                    vertices.Add(vtx);
                    normals.Add(vtx.Normalized());

                    float u2 = 0.0f, v2 = 0.0f;

                    if (i == 0) // bottom-left
                    {
                        u2 = lon - halfLonRange;
                        v2 = lat - halfLatRange;
                    }
                    else if (i == 1) // bottom-right
                    {
                        u2 = lon + halfLonRange;
                        v2 = lat - halfLatRange;
                    }
                    else if (i == 2) // top-right
                    {
                        u2 = lon + halfLonRange;
                        v2 = lat + halfLatRange;
                    }
                    else if (i == 3) // top-left
                    {
                        u2 = lon - halfLonRange;
                        v2 = lat + halfLatRange;
                    }

                    uv2s.Add(new Vector2(u2, v2));
                }

                // Two triangles for the quad with clockwise winding:
                // First triangle: [0, 2, 1]
                indices.Add(0);
                indices.Add(2);
                indices.Add(1);

                // Second triangle: [0, 3, 2]
                indices.Add(0);
                indices.Add(3);
                indices.Add(2);
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
            surfaceArray[(int)Mesh.ArrayType.TexUV2] = uv2s.ToArray();
            surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

            var arrayMesh = new ArrayMesh();
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
            return arrayMesh;
        }
}
