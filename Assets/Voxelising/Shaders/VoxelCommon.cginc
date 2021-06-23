uniform float3 _VX_BoundsMin;
uniform float3 _VX_BoundsMax;
uniform float3 _VX_BoundsSize;
uniform float3 _VX_BoundsProportions;
uniform float  _VX_BoundsMaxDimension;

struct vx_appdata
{
    float4 vertex : POSITION;
};

struct vx_varyings
{
    float4 vertex : SV_POSITION;
    float3 uv : TEXCOORD0;
    float3 vectorToSurface : TEXCOORD1;
};

vx_varyings VX_VertexStage(vx_appdata v)
{
    vx_varyings o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    // Sample origin is normalised object space position
    o.uv = (v.vertex - _VX_BoundsMin) / _VX_BoundsSize;

    // Calculate vector from camera to vertex in world space
    float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
    o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

    return o;
}

float3 VX_TextureSpaceViewDirection(float3 vectorToSurface)
{
    // Use vector from camera to object surface to get ray direction
    float3 rayDirection = mul(unity_WorldToObject, float4(normalize(vectorToSurface), 0));

    // Scale from model proportions to uniform proportions
    return rayDirection / _VX_BoundsProportions;
}