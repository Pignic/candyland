sampler BaseSampler : register(s0);
sampler VariationSampler : register(s1);

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR
{
    float4 basePixel = tex2D(BaseSampler, uv);
    float4 varPixel  = tex2D(VariationSampler, uv);

    // If base is transparent, variation must be completely invisible
    if (basePixel.a == 0)
    {
        return float4(0, 0, 0, 0);
    }

    // Apply variation only where base is opaque
    float3 resultRGB = basePixel.rgb + (varPixel.rgb * varPixel.a);

    // Final alpha is ALWAYS the base alpha
    float resultA = basePixel.a;

    return float4(resultRGB, resultA);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
}