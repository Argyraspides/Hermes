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
 * towards a specified haze color.
 */

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back;

// Constants
const float TWO_PI = 6.28318530718;
const float QUARTER_PI = 0.78539816339;

// Uniforms for Map Projection
uniform sampler2D map_tile : source_color, filter_linear_mipmap, repeat_disable;
uniform int zoom_level = 1;
uniform int tile_height = 256;
uniform int tile_width = 256;

// Uniforms for Atmospheric Haze (Distance Based)
uniform vec4 	haze_color : source_color = vec4(0.3, 0.5, 1.0, 1.0); 		// More vibrant blue default
uniform float 	haze_density : hint_range(0.0, 1.0) = 0.01; 				// Start low for smoothstep
uniform float 	haze_start_distance : hint_range(0.0, 10000.0) = 600.0; 	// Increased default

// Uniforms for Atmospheric Haze (Limb Effect Based)
uniform float limb_intensity : hint_range(0.0, 1.0) = 0.99; // How strong the limb effect is
uniform float limb_exponent : hint_range(0.1, 8.0) = 3.0;  // How sharp the limb transition is (higher = sharper)

// Varyings
varying vec2 vertex_lat_lon;
varying vec3 world_pos;
varying vec3 world_normal; // Pass world normal to fragment shader

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

void vertex() {
    vertex_lat_lon = UV2;
    world_pos = VERTEX;
    world_normal = NORMAL; // Store the world-space normal
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

    // --- Atmospheric Haze Calculation ---
    vec3 view_dir = normalize(world_pos - CAMERA_POSITION_WORLD);
    float dist = length(world_pos - CAMERA_POSITION_WORLD); // equivalent to distance()

    // 1. Distance-based haze (using smoothstep)
    float transition_length = 7000.0 / max(haze_density, 0.0001);
    float haze_end_distance = haze_start_distance + transition_length;
    float distance_haze_amount = smoothstep(haze_start_distance, haze_end_distance, dist);

    // 2. Limb-based haze factor
    // dot_prod is ~ -1 facing camera, ~ 0 at limb
    float dot_prod = dot(view_dir, normalize(world_normal));

    // limb_factor is ~ 0 facing camera, ~ 1 at limb
    float limb_factor = 1.0 - max(0.0, -dot_prod);
    limb_factor = pow(limb_factor, limb_exponent);

    float final_haze_amount = max(distance_haze_amount, limb_factor);

    // --- Apply Haze ---
    vec3 final_color = mix(map_color.rgb, haze_color.rgb, final_haze_amount);

    // --- Final Output ---
    ALBEDO = final_color;
    ALPHA = map_color.a;
}
