using System.Collections.Generic;
using System.Linq;
using Godot;
using Hermes.Common.Map.Utils;

namespace Hermes.Common.Meshes.MeshGenerators;

public class WebMercatorMeshGenerator
{
    public static ArrayMesh CreateMeshSegment(float lat, float lon, float latRange, float lonRange)
    {
        var surfaceArray = new Godot.Collections.Array();
        surfaceArray.Resize((int)Mesh.ArrayType.Max);

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var indices = new List<int>();

        // Half-ranges:
        float halfLatRange = latRange / 2.0f;
        float halfLonRange = lonRange / 2.0f;

        bool touchesNorthPole = (lat + halfLatRange) >= (Mathf.Pi / 2.0f);
        bool touchesSouthPole = (lat - halfLatRange) <= (-Mathf.Pi / 2.0f);

        // Calculate the minor-to-major axis ratio
        float minorToMajorRatio = (float)(SolarSystemConstants.EARTH_POLAR_RADIUS_KM /
                                          SolarSystemConstants.EARTH_EQUATORIAL_RADIUS_KM);


        for (int i = 0; i < 4; i++)
        {

            Vector2 webMercatorCoordinates = new Vector2();

            switch (i)
            {
                case 0: // Top left corner of the tile

                    webMercatorCoordinates = MapUtils.LatLonToCartesianWebMercator(lat + halfLatRange, lon - halfLonRange);
                    break;
                case 1: // Top right corner of the tile
                    webMercatorCoordinates = MapUtils.LatLonToCartesianWebMercator(lat + halfLatRange, lon + halfLonRange);
                    break;
                case 2: // Bottom right corner of the tile
                    webMercatorCoordinates = MapUtils.LatLonToCartesianWebMercator(lat - halfLatRange, lon + halfLonRange);
                    break;
                case 3: // Bottom left corner of the tile
                    webMercatorCoordinates = MapUtils.LatLonToCartesianWebMercator(lat - halfLatRange, lon - halfLonRange);
                    break;
            }

            float[] x =
            {
                webMercatorCoordinates.X
            };
            float[] y =
            {

            };
            float[] z =
            {

            };
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
        surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var arrayMesh = new ArrayMesh();
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
        return arrayMesh;
    }
}
