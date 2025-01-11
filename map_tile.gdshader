shader_type spatial;

uniform sampler2D tile_texture;
uniform vec2 uv_min;
uniform vec2 uv_max;

void fragment() {
    // Default color (for debugging)
    ALBEDO = vec3(1.0, 0.0, 0.0); // Red base color
    
    // Check if current UV is within our tile bounds
    if (UV.x >= uv_min.x && UV.x <= uv_max.x && 
        UV.y >= uv_min.y && UV.y <= uv_max.y) {
        vec2 remapped_uv = (UV - uv_min) / (uv_max - uv_min);
        vec4 tile = texture(tile_texture, remapped_uv);
        ALBEDO = tile.rgb;
        ALPHA = tile.a;
    }
}