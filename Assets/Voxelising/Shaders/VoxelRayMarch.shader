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
            
            // Integers required for raymarch loop to be dynamic based on input voxel resolution
            #pragma require integers
            
            // Jitter sample position for smoothing when used with TAA
            #pragma shader_feature VX_NOISE_JITTER

            #include "UnityCG.cginc"
            #include "VoxelCommon.cginc"

            #pragma vertex VX_VertexStage
            #pragma fragment frag

            sampler3D _MainTex;

            #ifdef VX_NOISE_JITTER
            sampler2D _NoiseTex;
            float4 _NoiseTex_TexelSize;
            uniform float2 _VX_NoiseFrameOffset;
            #endif

            uniform uint _VX_RaymarchStepCount;

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f

            fixed4 frag (vx_varyings i) : SV_Target
            {
                #ifdef VX_NOISE_JITTER
                float2 noiseUV = i.vertex.xy * _ScreenParams.x * _NoiseTex_TexelSize.xy + _VX_NoiseFrameOffset;
                fixed noiseValue = tex2D(_NoiseTex, noiseUV);
                #endif

                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.uv;

                float3 rayDirection = VX_TextureSpaceViewDirection(i.vectorToSurface);
                
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
