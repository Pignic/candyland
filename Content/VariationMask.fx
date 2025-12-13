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
float4 BaseSourceRect;      // (x, y, width, height)
float2 TextureSize;         // Size of the tileset texture (in pixels)
float TileSize;             // Size of one tile in pixels (e.g., 64)

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;        // ← Used to pass tile coordinates!
    float2 TexCoord : TEXCOORD0;
};

// Constants
static const float ALPHA_THRESHOLD = 0.01;

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 baseColor = tex2D(TextureSampler, input.TexCoord);
    
    if (baseColor.a < ALPHA_THRESHOLD)
        return float4(0, 0, 0, 0);
    
    // ========================================================================
    // DECODE ALL PARAMETERS FROM COLOR
    // ========================================================================
    
    // Decode tile coordinates (for variation)
    int tileX = (int)floor(input.Color.r * 255.0 / 16.0 + 0.5);
    int tileY = (int)floor(input.Color.g * 255.0 / 16.0 + 0.5);
    
    // Decode source rect position
    float sourceColF = input.Color.b * 255.0;
    float sourceRowF = input.Color.a * 255.0;
    int sourceCol = (int)floor(sourceColF / 64.0 + 0.5);  // 0, 1, 2, or 3
    int sourceRow = (int)floor(sourceRowF / 64.0 + 0.5);  // 0, 1, 2, or 3
    
    // Reconstruct BaseSourceRect
    float4 BaseSourceRect = float4(
        sourceCol * TileSize,   // X
        sourceRow * TileSize,   // Y
        TileSize,               // Width
        TileSize                // Height
    );
    
    // ========================================================================
    // REST OF SHADER
    // ========================================================================
    
    // Calculate local position
    float2 baseRectMin = BaseSourceRect.xy / TextureSize;
    float2 baseRectMax = (BaseSourceRect.xy + BaseSourceRect.zw) / TextureSize;
    float2 baseRectSize = baseRectMax - baseRectMin;
    
    if (baseRectSize.x < 0.0001 || baseRectSize.y < 0.0001)
        return baseColor;
    
    float2 localPos = (input.TexCoord - baseRectMin) / baseRectSize;
    localPos = saturate(localPos);
    
    // Calculate variation
    int variationIndex = ((tileX * 7 + tileY * 13) % 4);
    
    float4 VariationSourceRect = float4(
        variationIndex * TileSize,
        TileSize * 4,
        TileSize,
        TileSize
    );
    
    // Map to variation tile
    float2 varRectMin = VariationSourceRect.xy / TextureSize;
    float2 varRectSize = VariationSourceRect.zw / TextureSize;
    float2 varUV = varRectMin + (localPos * varRectSize);
    
    // Sample and blend
    float4 varColor = tex2D(TextureSampler, varUV);
    
    if (varColor.a > ALPHA_THRESHOLD)
    {
        float3 blendedRGB = lerp(baseColor.rgb, varColor.rgb, varColor.a);
        return float4(blendedRGB, baseColor.a);
    }
    else
    {
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
