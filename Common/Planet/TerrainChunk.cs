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

public partial class TerrainChunk : Node
{

	public TerrainChunk(
		float lat,
		float lon,
		float latRange,
		float lonRange,
		MeshInstance3D meshInstance3D = null,
		Texture2D texture2D = null)
	{
		m_latitude = lat;
		m_longitude = lon;
		m_latitudeRange = latRange;
		m_longitudeRange = lonRange;
		m_meshInstance3D = meshInstance3D;
		m_texture2D = texture2D;


		if(m_texture2D != null)
		{
			StandardMaterial3D material3D = new StandardMaterial3D();
			material3D.AlbedoTexture = m_texture2D;
			m_meshInstance3D.MaterialOverride = material3D;
			AddChild(meshInstance3D);
		}

	}

	// Represents the latitude location of this terrain chunk.
	// Latitude is located at the center of the chunk's shape
	// which is determined by its mesh
	public float Latitude => m_latitude;

	// Represents the latitude range of this terrain chunk.
	// I.e., how  many degrees of latitude that the chunk covers
	// in total
	public float LatitudeRange => m_latitudeRange;

	// Represents the longitude location of this terrain chunk.
	// Latitude is located at the center of the chunk's shape
	// which is determined by its mesh
	public float Longitude => m_longitude;

	// Represents the longitude range of this terrain chunk.
	// I.e., how  many degrees of longitude that the chunk covers
	// in total
	public float LongitudeRange => m_longitudeRange;


	// The mesh that will define the geometry of the chunk.
	// In general, if the mesh includes the poles of the planet,
	// the mesh will be triangular. Otherwise, it will be a quadrilateral
	public MeshInstance3D MeshInstance => m_meshInstance3D;


	// This shader is used for map reprojection.
	// E.g., warping a Web-Mercator projection map tile
	// such that it can be fit to an ellipsoid
	public ShaderMaterial ShaderMaterial => m_shaderMaterial;


	// A TerrainChunk is inherently a quadtree, so these hold the four children if
	// they exist
	// 0 = Top left
	// 1 = Top right
	// 2 = Bottom left
	// 3 = Bottom right
	TerrainChunk[] children = new TerrainChunk[4];


	private float m_latitude;
	private float m_latitudeRange;
	private float m_longitude;
	private float m_longitudeRange;

	private MeshInstance3D m_meshInstance3D;
	private Texture2D m_texture2D;
	private ShaderMaterial m_shaderMaterial;

}
