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
    // input.TexCoord is ALREADY mapped to the base tile by SpriteBatch
    // Don't recalculate it!
    float4 baseColor = tex2D(TextureSampler, input.TexCoord);
    
    // For variation: map from tile-local coords to variation tile position
    float2 pixelPos = input.TexCoord * float2(BaseSourceRect.z, BaseSourceRect.w);
    float2 varUV = (VariationSourceRect.xy + pixelPos) / TextureSize;
    float4 varColor = tex2D(TextureSampler, varUV);
    
    // Apply variation where base is opaque
    if (baseColor.a > 0.01)
    {
        // Only blend where variation actually has color (not transparent)
        if (varColor.a > 0.01)
        {
            // Blend the variation color
            float3 blended = lerp(baseColor.rgb, varColor.rgb, varColor.a);
            return float4(blended, baseColor.a);
        }
        else
        {
            // Variation is transparent here, just return base
            return baseColor;
        }
    }
    else
    {
        // Base is transparent
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
