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

/* 
 * Web Mercator Map Tile Projection Shader
 * 
 * This shader projects a 256x256 Web-Mercator map tile onto a WGS84 ellipsoid mesh.
 * Each mesh vertex's latitude/longitude (in radians) is stored in the UV2 array.
 * The shader determines which pixel of the map tile corresponds to this lat/lon
 * and applies it to the mesh material.
 */

shader_type spatial;

// Constants
const float PI = 3.14159265359;
const float HALF_PI = 1.57079632679;
const int TILE_SIZE = 256;

// Uniforms
uniform sampler2D map_tile;
uniform int zoom_level;

// Varying
varying vec2 vertex_lat_lon;

// Converts latitude to pixel coordinate on the specific map tile
// Reference: https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Web_Mercator
float lat_to_pixel(float lat) {
    float y_mercator = log(tan(HALF_PI + lat * 0.5));
    float y = 0.5 - y_mercator / (2.0 * PI);
    
    float n = pow(2.0, float(zoom_level));
    float y_tile = n * y;
    float frac_tile = y_tile - floor(y_tile);
    
    return frac_tile * float(TILE_SIZE);
}

// Converts longitude to pixel coordinate on the specific map tile
// Reference: https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Web_Mercator
float lon_to_pixel(float lon) {
    float lon_deg = lon * (180.0 / PI);
    float x_mercator = lon_deg;
    float x = 0.5 + x_mercator / 360.0;
    
    float n = pow(2.0, float(zoom_level));
    float x_tile = n * x;
    float frac_tile = x_tile - floor(x_tile);
    
    return frac_tile * float(TILE_SIZE);
}

void vertex() {
    vertex_lat_lon = UV2;
}

void fragment() {
    // Extract latitude and longitude from mesh coordinates
    float lat = vertex_lat_lon.y;
    float lon = vertex_lat_lon.x;
    
    // Calculate pixel coordinates on the map tile
    float px = lon_to_pixel(lon);
    float py = lat_to_pixel(lat);
    
    // Convert to UV coordinates (0 to 1 range)
    vec2 uv = vec2(px, py) / float(TILE_SIZE);
    uv = clamp(uv, 0.0, 1.0);
    
    // Sample and apply the map tile texture
    vec4 color = texture(map_tile, uv);
    ALBEDO = color.rgb;
    ALPHA = color.a;
}