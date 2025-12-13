#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

// Texture passed via SpriteBatch (the tileset)
sampler TextureSampler : register(s0);

// Source rectangle for base tile in the tileset (in pixels)
// NOTE: This must still be set, but only BaseSourceRect changes per tile now
float4 BaseSourceRect;      // (x, y, width, height)
float2 TextureSize;         // Size of the tileset texture (in pixels)
float TileSize;             // Size of one tile in pixels (e.g., 64)

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;        // ← We use this to pass tile coordinates!
    float2 TexCoord : TEXCOORD0;
};

// Constants
static const float ALPHA_THRESHOLD = 0.01;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // ========================================================================
    // STEP 1: Sample the base tile
    // ========================================================================
    float4 baseColor = tex2D(TextureSampler, input.TexCoord);
    
    // Early exit if base is transparent
    if (baseColor.a < ALPHA_THRESHOLD)
    {
        return float4(0, 0, 0, 0);
    }
    
    // ========================================================================
    // STEP 2: Calculate local position within the base tile (0 to 1)
    // ========================================================================
    float2 baseRectMin = BaseSourceRect.xy / TextureSize;
    float2 baseRectMax = (BaseSourceRect.xy + BaseSourceRect.zw) / TextureSize;
    float2 baseRectSize = baseRectMax - baseRectMin;
    
    if (baseRectSize.x < 0.0001 || baseRectSize.y < 0.0001)
    {
        return baseColor;
    }
    
    float2 localPos = (input.TexCoord - baseRectMin) / baseRectSize;
    localPos = saturate(localPos);
    
    // ========================================================================
    // STEP 3: Decode tile coordinates from Color channel
    // ========================================================================
    // The C# code encodes tile coordinates in the Red and Green channels

int tileX = (int)floor(input.Color.r * 255.0 / 16.0 + 0.5);
int tileY = (int)floor(input.Color.g * 255.0 / 16.0 + 0.5);
    
    // Calculate variation index using the same formula as C#
    int variationIndex = ((tileX * 7 + tileY * 13) % 4);
    
    // ========================================================================
    // STEP 4: Calculate variation tile source rectangle
    // ========================================================================
    // Variation tiles are in row 4 (y = TileSize * 4)
    float4 VariationSourceRect = float4(
        variationIndex * TileSize,  // x
        TileSize * 4,                // y (row 4)
        TileSize,                     // width
        TileSize                      // height
    );
    
    // ========================================================================
    // STEP 5: Map local position to variation tile
    // ========================================================================
    float2 varRectMin = VariationSourceRect.xy / TextureSize;
    float2 varRectSize = VariationSourceRect.zw / TextureSize;
    float2 varUV = varRectMin + (localPos * varRectSize);
    
    // ========================================================================
    // STEP 6: Sample and blend variation
    // ========================================================================
    float4 varColor = tex2D(TextureSampler, varUV);
    
    if (varColor.a > ALPHA_THRESHOLD)
    {
        // Blend variation onto base, preserving base alpha
        float3 blendedRGB = lerp(baseColor.rgb, varColor.rgb, varColor.a);
        return float4(blendedRGB, baseColor.a);
    }
    else
    {
        // No variation at this pixel, return base
        return baseColor;
    }
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
