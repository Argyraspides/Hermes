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

/**

This shader takes in a Web-Mercator map tile of 256x256 pixels at a particular zoom level
and then warps it such that it is accurately projected onto a WGS84 ellipsoid mesh. Each mesh's
vertices has its latitude/longitude stored in the UV2 array. We find out which pixel of the map
tile corresponds to this lat/lon and then apply it to the mesh material. Latitude and longitude
values in the UV2 array are in radians

*/

// TODO: Parametrize tile width and height -- do not assume 256x256
shader_type spatial;

uniform sampler2D mapTile;
uniform int zoomLevel;

varying vec2 vertexLatLon;

int latToPixel(float lat)
{
	int tilesPerSide = (1 << zoomLevel);

	float tileColumn = float(tilesPerSide) * (1.0 - (log(tan(lat) + (1.0/cos(lat))) / PI)) / 2.0;
	float yPixel = tileColumn * 256.0;

	// Find which pixel 'y' is on our actual map tile. Map tiles are 256x256
	int py = int(yPixel) % 256;
	return py;
}

int lonToPixel(float lon)
{
	int tilesPerSide = (1 << zoomLevel);

	float lonDeg = lon * (180.0 / PI);
	float tileRow = float(tilesPerSide) * ((lonDeg + 180.0) / 360.0);
	float xPixel = tileRow * 256.0;

	// Find which pixel 'x' is on our actual map tile's local coordinate system.
	// Map tiles are 256x256
	int px = int(xPixel) % 256;

	return px;
}

void vertex() {
    // Pass through the lat/lon coordinates
    vertexLatLon = UV2;
}

void fragment() {

	// Latitude and longitude on our actual mesh
    float lat = vertexLatLon.y;
    float lon = vertexLatLon.x;

	// Pixel coordinate on the map tile corresponding to the latitude
	// and longitude
    int py = latToPixel(lat);
    int px = lonToPixel(lon);

    // Convert pixel coordinates to UV coordinates (0 to 1 range)
    vec2 uv = vec2(float(px) / 256.0, float(py) / 256.0);

    // Sample the map tile at the calculated UV position
    vec4 color = texture(mapTile, uv);
	// ALBEDO = vec3(vertexLatLon.x, vertexLatLon.x, 0.0);

    ALBEDO = color.rgb;
    ALPHA = color.a;
}