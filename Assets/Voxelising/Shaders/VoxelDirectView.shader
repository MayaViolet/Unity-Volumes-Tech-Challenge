Shader "Voxels/VoxelDirectView"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex ("Texture", 3D) = "white" {}
        [NoScaleOffset]
        _NoiseTex ("Noise", 2D) = "black" {}
        [Toggle(VX_NOISE_JITTER)]
        _ToggleNoiseJitter ("Use noise jitter", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature VX_NOISE_JITTER

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                float3 vectorToSurface : TEXCOORD1;
            };

            sampler3D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NoiseTex;
            uniform float2 _VX_NoiseFrameOffset;

            uniform float3 _VX_BoundsMin;
            uniform float3 _VX_BoundsMax;
            uniform float3 _VX_BoundsSize;
            uniform float3 _VX_BoundsProportions;
            uniform float  _VX_BoundsMaxDimension;

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f
            #define STEP_COUNT 192

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 _testSize = float3(1.0f, 1.0f, 1.0f);
                float3 _testMin = float3(-0.5f, -0.5f, -0.5f);
                o.uv = (v.vertex - _VX_BoundsMin) / _VX_BoundsSize;

                // Calculate vector from camera to vertex in world space
                float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

                return o;
            }

            float4 BlendUnder(float4 color, float4 newColor)
            {
                color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
                color.a += (1.0 - color.a) * newColor.a;
                return color;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef VX_NOISE_JITTER
                float2 noiseUV = i.vertex.xy * _ScreenParams.x / 16.0f + _VX_NoiseFrameOffset;
                fixed noiseValue = tex2D(_NoiseTex, noiseUV);
                #endif

                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.uv;

                // Use vector from camera to object surface to get ray direction
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));

                // Scale from model scale to uniform cube scale
                rayDirection = rayDirection / _VX_BoundsProportions;
                float stepSize = (1.0f / STEP_COUNT) * 1.5f;
                rayDirection *= stepSize;

                float4 color = float4(0, 0, 0, 0);
                float3 samplePosition = rayOrigin;
                #ifdef VX_NOISE_JITTER
                samplePosition += rayDirection * noiseValue;
                #endif

                // Raymarch through object space
                float4 best = float4(0,0,0,0);
                for (int i = 0; i < STEP_COUNT; i++)
                {
                    // Accumulate color only within unit cube bounds
                    if(max(abs(samplePosition.x-0.5f), max(abs(samplePosition.y-0.5f), abs(samplePosition.z-0.5f))) < 0.5f + EPSILON)
                    {
                        float4 sampledColor = tex3D(_MainTex, samplePosition);
                        if (sampledColor.a > best.a)
                        {
                            best = sampledColor;
                        }
                        sampledColor.a *= 0.01f;
                        color += sampledColor * sampledColor.a;
                        samplePosition += rayDirection;
                    }
                }

                clip(best.a - 0.1f);
                return best;
            }
            ENDCG
        }
    }
}
