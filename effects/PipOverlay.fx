// Only the PiP rectangle is rasterized. No scene sampling or full-screen filter.
uniform float4 PipRect = float4(0.02, 0.02, 0.25, 0.25);
uniform float2 PipUV = float2(1, 1);
texture2D PipVideo { Width = 1920; Height = 1080; Format = RGBA8; };
sampler2D PipSampler { Texture = PipVideo; MinFilter = LINEAR; MagFilter = LINEAR; MipFilter = POINT; AddressU = CLAMP; AddressV = CLAMP; };
void PipVertex(uint id : SV_VertexID, out float4 pos : SV_Position, out float2 uv : TEXCOORD0)
{
    // Two triangles, with top-left normalized coordinates matching the editor.
    float2 corner = id == 0 ? float2(0,0) : id == 1 ? float2(1,0) : id == 2 ? float2(0,1) : id == 3 ? float2(0,1) : id == 4 ? float2(1,0) : float2(1,1);
    pos = float4((PipRect.xy + corner * PipRect.zw) * float2(2,-2) + float2(-1,1), 0, 1);
    uv = corner * PipUV;
}
float4 PipPixel(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
    // The shared capture buffer is BGRA, while the effect texture is RGBA.
    return float4(tex2D(PipSampler, clamp(uv, float2(0.5/1920.0,0.5/1080.0), PipUV-float2(0.5/1920.0,0.5/1080.0))).bgr, 1);
}
technique PiPOverlay
{
    pass { VertexShader = PipVertex; PixelShader = PipPixel; VertexCount = 6; ClearRenderTargets = false; BlendEnable = false; }
}
