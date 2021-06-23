Shader "Voxels/VoxelSDF"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex ("Texture", 3D) = "white" {}
        [NoScaleOffset]
        _SDFTex ("Distance Field Texture", 3D) = "black" {}
        _DistanceScale ("Distance scale", float) = 1
        _SurfaceOffset ("Surface offset", float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #include "UnityCG.cginc"
            #include "VoxelCommon.cginc"

            #pragma vertex VX_VertexStage
            #pragma fragment frag

            sampler3D _MainTex;

            sampler3D _SDFTex;
            float _DistanceScale;
            float _SurfaceOffset;

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f
            #define STEP_COUNT 8

            fixed4 frag (vx_varyings i) : SV_Target
            {

                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.uv;

                // Use vector from camera to object surface to get ray direction
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));

                // Scale from model proportions to uniform proportions
                rayDirection = rayDirection / _VX_BoundsProportions;

                float3 samplePosition = rayOrigin;
                half sphereDistance = 1.0f;
                // Spheremarch using nearest distance
                for (int i = 0; i < STEP_COUNT; i++)
                {
                    //if (sphereDistance > EPSILON)
                    {
                        sphereDistance = tex3D(_SDFTex, samplePosition).r - _SurfaceOffset;
                        samplePosition += rayDirection * max(0, sphereDistance +0.5f/STEP_COUNT) * _DistanceScale;
                    }
                }

                if (max(abs(samplePosition.x-0.5f), max(abs(samplePosition.y-0.5f), abs(samplePosition.z-0.5f))) > 0.5f + EPSILON)
                {
                    sphereDistance = 10.0f;
                }
                
                // Sample color texture from where we landed
                float4 sampledColor = tex3D(_MainTex, samplePosition);

                clip(EPSILON - sphereDistance);
                return sampledColor;
            }
            ENDCG
        }
    }
}
