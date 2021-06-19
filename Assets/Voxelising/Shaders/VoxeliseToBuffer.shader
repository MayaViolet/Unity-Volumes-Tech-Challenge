Shader "Voxels/_VoxeliseToBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off ZWrite Off ZTest Always Fog{ Mode Off }
            
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            RWStructuredBuffer<float4> _VoxelUAV : register(u2);
            uniform int _Res;
            uniform int _MetaRes;

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 outV : COLOR0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.outV.xyz = o.vertex.xyz;
                o.outV.w = 1;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float rand_1_05(in float2 uv)
            {
                float2 noise = (frac(sin(dot(uv ,float2(12.9898,78.233)*2.0)) * 43758.5453));
                return abs(noise.x + noise.y) * 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                //col.rg = i.outV.xy;
                col.a = 1;

                float3 outCoords = i.outV.xyz;
                outCoords.y *= -1;
                uint3 outInd = floor(saturate(outCoords)*_Res);
                uint indexXY = outInd.x + outInd.y * _MetaRes * _Res;
                uint2 zTile = uint2(outInd.z%_MetaRes, floor(outInd.z/_MetaRes));
                zTile.y = _MetaRes - zTile.y - 1;
                uint indexZ = (zTile.x + zTile.y * _MetaRes * _Res) * _Res;
                _VoxelUAV[indexXY+indexZ] = col;
                return col;
            }
            ENDCG
        }
    }
}
