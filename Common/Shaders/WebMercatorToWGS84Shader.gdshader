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


// Converts a line of latitude to a pixel coordinate on this specific map tile
// Formula reference: 
// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
float latToPixel(float lat)
{
	float y_epsg3857 = log(tan( (PI / 4.0) + lat * 0.5 ));

	float y = 0.5 - y_epsg3857 / (2.0 * PI);

	float n = pow(2.0, float(zoomLevel));
	float y_tile = n * y;
	float frac_tile = y_tile - floor(y_tile);
	
	float y_pixel = frac_tile * 256.0;
	return y_pixel;
}

// Converts a line of longitude to a pixel coordinate on this specific map tile
// Formula reference: 
// https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#2._Convert_the_coordinate_to_the_Web_Mercator_projection_(https://epsg.io/3857)
float lonToPixel(float lon)
{
	float lonDeg = lon * (180.0 / PI);
	float x_epsg3857 = lonDeg;
	float x = 0.5 + x_epsg3857 / 360.0;

	float n = pow(2.0, float(zoomLevel));

	float x_tile = n * x;
	float frac_tile = x_tile - floor(x_tile);
	
	float x_pixel = frac_tile * 256.0;
	return x_pixel;
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
    float py = latToPixel(lat);
    float px = lonToPixel(lon);

    // Convert pixel coordinates to UV coordinates (0 to 1 range)
    vec2 uv = vec2(px / 256.0, py / 256.0);
	uv = clamp(uv, 0.0, 1.0);

    // Sample the map tile at the calculated UV position
    vec4 color = texture(mapTile, uv);

    ALBEDO = color.rgb;
    ALPHA = color.a;
}