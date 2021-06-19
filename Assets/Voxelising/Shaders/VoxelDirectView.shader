Shader "Voxels/VoxelDirectView"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
        _SliceZ ("Slice Z", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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

            float _SliceZ;

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                //o.uv.z = _SliceZ;

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
                // sample the texture
                fixed4 col = tex3D(_MainTex, i.uv);
                //return col;

                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.uv;

                // Use vector from camera to object surface to get ray direction
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));

                float4 color = float4(0, 0, 0, 0);
                float3 samplePosition = rayOrigin;

                // Raymarch through object space
                float4 best = float4(0,0,0,0);
                for (int i = 0; i < 64; i++)
                {
                    // Accumulate color only within unit cube bounds
                    //if(max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
                    {
                        float4 sampledColor = tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
                        if (sampledColor.a > best.a)
                        {
                            best = sampledColor;
                        }
                        sampledColor.a *= 0.01;
                        color += sampledColor * sampledColor.a;
                        samplePosition += rayDirection * 0.02;
                    }
                }

                return best;
            }
            ENDCG
        }
    }
}
