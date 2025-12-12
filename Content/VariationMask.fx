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

// Source rectangles for base and variation in the tileset
float4 BaseSourceRect;      // (x, y, width, height) in pixels
float4 VariationSourceRect; // (x, y, width, height) in pixels
float2 TextureSize;         // Size of the tileset texture

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // input.TexCoord is 0-1 across the drawn quad
    
    // Calculate UVs for base tile
    float2 baseUV = float2(
        (BaseSourceRect.x + input.TexCoord.x * BaseSourceRect.z) / TextureSize.x,
        (BaseSourceRect.y + input.TexCoord.y * BaseSourceRect.w) / TextureSize.y
    );
    
    // Calculate UVs for variation tile
    float2 varUV = float2(
        (VariationSourceRect.x + input.TexCoord.x * VariationSourceRect.z) / TextureSize.x,
        (VariationSourceRect.y + input.TexCoord.y * VariationSourceRect.w) / TextureSize.y
    );
    
    // Sample both
    float4 baseColor = tex2D(TextureSampler, baseUV);
    float4 varColor = tex2D(TextureSampler, varUV);
    
    // Only apply variation where base has alpha
    if (baseColor.a > 0.01)
    {
        // Blend variation over base
        float varAlpha = varColor.a;
        float3 blended = lerp(baseColor.rgb, varColor.rgb, varAlpha);
        return float4(blended, baseColor.a);
    }
    else
    {
        // Base is transparent, stay transparent
        return float4(0, 0, 0, 0);
    }
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
