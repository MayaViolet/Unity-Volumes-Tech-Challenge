Shader "Voxels/VoxelRayMarch"
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

            // Integers required for raymarch loop to be dynamic based on input voxel resolution
            #pragma require integers
            
            // Jitter sample position for smoothing when used with TAA
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

            #ifdef VX_NOISE_JITTER
            sampler2D _NoiseTex;
            float4 _NoiseTex_TexelSize;
            uniform float2 _VX_NoiseFrameOffset;
            #endif

            uniform float3 _VX_BoundsMin;
            uniform float3 _VX_BoundsMax;
            uniform float3 _VX_BoundsSize;
            uniform float3 _VX_BoundsProportions;
            uniform float  _VX_BoundsMaxDimension;

            uniform uint _VX_RaymarchStepCount;

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = (v.vertex - _VX_BoundsMin) / _VX_BoundsSize;

                // Calculate vector from camera to vertex in world space
                float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                #ifdef VX_NOISE_JITTER
                float2 noiseUV = i.vertex.xy * _ScreenParams.x * _NoiseTex_TexelSize.xy + _VX_NoiseFrameOffset;
                fixed noiseValue = tex2D(_NoiseTex, noiseUV);
                #endif

                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.uv;

                // Use vector from camera to object surface to get ray direction
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 0));

                // Scale from model proportions to uniform proportions
                rayDirection = rayDirection / _VX_BoundsProportions;
                
                // Dividing our step vector by our longest axis gets the step size for one whole voxel
                float stepSize = (1.0f / _VX_RaymarchStepCount);
                float longestAxis = max(abs(rayDirection.x), max(abs(rayDirection.y), abs(rayDirection.z)));
                stepSize /= longestAxis;
                rayDirection *= stepSize;

                float3 samplePosition = rayOrigin;
                #ifdef VX_NOISE_JITTER
                samplePosition += rayDirection * noiseValue;
                #endif

                // Raymarch through object space
                float4 best = float4(0,0,0,0);
                for (int i = 0; i < _VX_RaymarchStepCount; i++)
                {
                    // Sample only within unit cube bounds
                    if(max(abs(samplePosition.x-0.5f), max(abs(samplePosition.y-0.5f), abs(samplePosition.z-0.5f))) < 0.5f + EPSILON)
                    {
                        float4 sampledColor = tex3D(_MainTex, samplePosition);
                        // Opaque rendering, so output the most opaque voxel encountered
                        if (sampledColor.a > best.a)
                        {
                            best = sampledColor;
                        }
                        samplePosition += rayDirection;
                    }
                }

                clip(best.a - EPSILON);
                return best;
            }
            ENDCG
        }
    }
}
