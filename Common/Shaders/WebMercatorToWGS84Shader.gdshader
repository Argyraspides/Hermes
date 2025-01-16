shader_type spatial;

uniform sampler2D mapTile;
uniform int zoomLevel;

varying vec2 vertexLatLon;

int latToPixel(float lat)
{
	int tilesPerSide = (1 << zoomLevel);
	
	// See: https://en.wikipedia.org/wiki/Web_Mercator_projection#Formulas
	// 'y' is the pixel on the map corresponding to the latitude (lat) at the 
	// particular zoom level (zoomLevel). Not a map tile, but the entire map.
	float y = floor((1.0 / 2.0*PI) * float(tilesPerSide) * (PI - log(tan(PI/4.0 + lat/2.0))));
	
	// Find which pixel 'y' is on our actual map tile. Map tiles are 256x256
	int px = int(y) % 256;
	return px;
}

int lonToPixel(float lon)
{
	int tilesPerSide = (1 << zoomLevel);
	
	// See: https://en.wikipedia.org/wiki/Web_Mercator_projection#Formulas
	// 'x' is the pixel on the map corresponding to the longitude (lon) at the 
	// particular zoom level (zoomLevel). Not a map tile, but the entire map.
	float x = (1.0/(2.0*PI)) * float(tilesPerSide) * (PI + lon);
	
	// Find which pixel 'x' is on our actual map tile. Map tiles are 256x256
	int px = int(x) % 256;
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
    
    ALBEDO = color.rgb;
    ALPHA = color.a;
}