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
            #pragma vertex vert
            #pragma fragment frag

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

            sampler3D _SDFTex;
            float4 _SDFTex_ST;
            float _DistanceScale;
            float _SurfaceOffset;

            uniform float3 _VX_BoundsMin;
            uniform float3 _VX_BoundsMax;
            uniform float3 _VX_BoundsSize;
            uniform float3 _VX_BoundsProportions;
            uniform float  _VX_BoundsMaxDimension;

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f
            #define STEP_COUNT 8

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

            fixed4 frag (v2f i) : SV_Target
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
