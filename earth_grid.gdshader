shader_type spatial;

uniform vec4 grid_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float line_width : hint_range(0.0, 0.1) = 0.01;
uniform vec2 subdivision_amount = vec2(64.0, 32.0);  // Will be updated from C#

void fragment() {
    vec2 uv = UV;
    
    vec2 grid = abs(fract(uv * subdivision_amount) - 0.5);
    float line = step(0.5 - line_width, max(grid.x, grid.y));
    
    ALBEDO = mix(vec3(0.0, 0.0, 0.2), grid_color.rgb, line);
    METALLIC = 0.0;
    ROUGHNESS = 1.0;
}
