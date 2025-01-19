# Godot Shader Style Guide (.gdshader)

## Table of Contents
1. File Organization
2. Naming Conventions
3. Formatting
4. Language Usage
5. Documentation
6. Performance Guidelines
7. Godot-Specific Features

## 1. File Organization

### File Structure
```glsl
/* File header comment describing the shader's purpose */
shader_type spatial; // or canvas_item, sky, etc.

// Render mode declarations
render_mode cull_disabled, depth_draw_opaque;

// Constants
const float PI = 3.14159265359;

// Uniform declarations
uniform vec4 albedo : source_color;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;

// Varying declarations
varying vec3 world_position;

// Include statements if needed
#include "common.gdshaderinc"

// Function declarations
void vertex() {
    // Vertex shader code
}

void fragment() {
    // Fragment shader code
}

// Custom functions
float custom_function() {
    // Function implementation
}
```

### File Names
- Use lowercase with underscores: `terrain_shader.gdshader`
- Include the shader type in name: `water_spatial.gdshader`
- Prefix with feature name: `dissolve_spatial.gdshader`

## 2. Naming Conventions

### Variables and Functions
- Uniforms use lowercase with underscores: `uniform float wind_strength;`
- Varyings use descriptive names with underscores: `varying vec3 world_normal;`
- Custom functions use lowercase with underscores: `float calculate_fresnel()`
- Constants use UPPER_SNAKE_CASE: `const float MAX_DISTANCE = 100.0;`

### Uniforms Hints and Groups
- Group related uniforms using the group hint:
```glsl
uniform vec4 albedo : source_color;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float roughness : hint_range(0.0, 1.0) = 0.5;
```

## 3. Formatting

### Braces and Indentation
- Opening brace on the same line
- 4 spaces for indentation (no tabs)
```glsl
void fragment() {
    vec4 color = albedo;
    if (use_pattern) {
        color *= calculate_pattern();
    }
}
```

### Line Length and Wrapping
- Soft limit of 80 characters per line
- Break long expressions at operators:
```glsl
float result = long_calculation_name(parameter1, parameter2) 
    * another_long_calculation(parameter3)
    + final_calculation();
```

### Spacing
- One space after keywords: `if (condition)`
- One space around operators: `a + b`
- No space before semicolons: `float x = 1.0;`
- One blank line between logical sections

## 4. Language Usage

### Vector Components
- Use appropriate swizzling for clarity:
```glsl
vec3 position = VERTEX;
float height = position.y;
vec2 uv_offset = position.xz;
```

### Constants
- Define commonly used values as constants:
```glsl
const vec3 UP = vec3(0.0, 1.0, 0.0);
const float TAU = 6.28318530718;
```

### Functions
- Keep functions focused and single-purpose
- Return early when possible
```glsl
float calculate_fade(float distance) {
    if (distance > MAX_DISTANCE) {
        return 0.0;
    }
    return 1.0 - (distance / MAX_DISTANCE);
}
```

## 5. Documentation

### Comments
- Start files with a descriptive header
- Document complex calculations
- Explain magic numbers
```glsl
/* 
 * Terrain shader with triplanar mapping and snow accumulation
 * Based on: [reference or explanation]
 */

// Fresnel calculation using Schlick's approximation
float fresnel = pow(1.0 - dot(NORMAL, VIEW), 5.0);

// 0.08 is the average specular reflection for non-metals
const float SPEC_CONST = 0.08;
```

### TODO Comments
- Include author and date
- Provide context
```glsl
// TODO(alice, 2025-01-20): Optimize noise calculation for mobile
```

## 6. Performance Guidelines

### Optimization Tips
- Precalculate values in vertex shader when possible
- Use built-in functions over custom implementations
- Minimize texture lookups
```glsl
void vertex() {
    // Precalculate world position for fragment shader
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    // Reuse precalculated value
    float distance = length(world_position);
}
```

### Conditional Statements
- Minimize branching in fragment shaders
- Use step/mix instead of if when possible
```glsl
// Instead of if/else
float result = mix(value1, value2, step(threshold, input));
```

## 7. Godot-Specific Features

### Built-in Variables
- Use Godot's built-in variables with proper capitalization:
```glsl
void vertex() {
    VERTEX += NORMAL * offset;
    UV = UV * uv_scale;
}

void fragment() {
    ALBEDO = albedo.rgb;
    ROUGHNESS = roughness;
    METALLIC = metallic;
}
```

### Render Modes
- Declare render modes at the top of the file
- Comment why non-standard modes are used
```glsl
// Enable alpha blending for transparency
render_mode blend_mix;
// Disable backface culling for two-sided materials
render_mode cull_disabled;
```

### Include Files
- Use includes for shared functionality
- Name include files with .gdshaderinc extension
```glsl
#include "noise_functions.gdshaderinc"
#include "common_utils.gdshaderinc"
```
