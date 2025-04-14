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
 * Web Mercator Map Tile Projection Shader with Atmospheric Haze & Limb Effect
 *
 * This shader projects a 256x256 Web-Mercator map tile onto a WGS84 ellipsoid mesh.
 * Each mesh vertex's latitude/longitude (in radians) is stored in the UV2 array.
 * The shader determines which pixel of the map tile corresponds to this lat/lon
 * and applies it to the mesh material.
 *
 * Includes an atmospheric haze effect that blends distant parts of the mesh
 * towards a specified haze color. To learn exactly how the atmospheric haze works,
 * see: https://vault-of-shields.vercel.app/On-Hermes/UI-Effects/Atmospheric-Limb
 */

shader_type spatial;
render_mode cull_back;

const float TWO_PI = 6.28318530718;
const float QUARTER_PI = 0.78539816339;

uniform sampler2D map_tile : source_color, filter_linear_mipmap, repeat_disable;

uniform int zoom_level = 1;
uniform int tile_height = 256;
uniform int tile_width = 256;

// Uniforms for Atmospheric Haze (Limb Effect Based)
uniform vec4 limb_color : source_color = vec4(0.3, 0.5, 1.0, 1.0);
uniform float limb_intensity : hint_range(0.0, 3.0) = 1.5;
uniform float limb_exponent : hint_range(0.1, 7.0) = 7.0;

// Past this zoom level we won't show the limb effect anymore.
// At very high zoom levels this messes with the appearance of houses and stuff
const int MAX_LIMB_ZOOM = 10;

varying vec2 vertex_lat_lon;
varying vec3 world_pos;
varying vec3 world_normal;
varying vec3 camera_position;

// Converts latitude to pixel coordinate on the specific map tile
// Reference: https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Web_Mercator
float lat_to_pixel(float lat) {
    float y_mercator = log(tan(QUARTER_PI + lat * 0.5));
    float y = 0.5 - y_mercator / TWO_PI;

    float n = float(1 << zoom_level);
    float y_tile = n * y;
    float frac_tile = y_tile - floor(y_tile);

    return frac_tile * float(tile_height);
}

// Converts longitude to pixel coordinate on the specific map tile
// Reference: https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Web_Mercator
float lon_to_pixel(float lon) {
	float x = 0.5 + lon / TWO_PI;

    float n = float(1 << zoom_level);
    float x_tile = n * x;
    float frac_tile = x_tile - floor(x_tile);

    return frac_tile * float(tile_width);
}

// Calculates the amount of "haze" we should apply for a particular
// pixel. See https://vault-of-shields.vercel.app/On-Hermes/UI-Effects/Atmospheric-Limb
// for how this works
float get_limb_intensity()
{
	// --- Atmospheric Haze Calculation ---
    vec3 view_dir = normalize(world_pos - camera_position);
    float dot_prod = dot(view_dir, normalize(world_normal));
    float limb_factor = 1.0 - max(0.0, -dot_prod);
    limb_factor = pow(limb_factor * limb_intensity, limb_exponent);
	return limb_factor;
}

void vertex() {
    vertex_lat_lon = UV2;
    world_pos = VERTEX;
    world_normal = NORMAL;
	camera_position = CAMERA_POSITION_WORLD;
}

void fragment() {

    // --- Map Projection ---
    float lat = vertex_lat_lon.y;
    float lon = vertex_lat_lon.x;
    float px = lon_to_pixel(lon);
    float py = lat_to_pixel(lat);
    vec2 uv = vec2(px / float(tile_width), py / float(tile_height));
    uv = clamp(uv, 0.0001, 0.9999);

    vec4 map_color = texture(map_tile, uv);

	if(zoom_level < MAX_LIMB_ZOOM)
	{
	    	vec3 final_color = mix(map_color.rgb, limb_color.rgb, get_limb_intensity());
	    	ALBEDO = final_color;
	}
	else
	{
		ALBEDO = map_color.rgb;
	}

    ALPHA = map_color.a;
}
