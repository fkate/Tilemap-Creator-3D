// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

Shader "Hidden/OrientationGizmo" {
    Properties {
        _MainTex ("Font", CUBE) = "black"
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertIn {
                float4 vertex : POSITION;
            };

            struct vertOut {
                float4 position : SV_POSITION;
                float3 texcoord : TEXCOORD;
            };

            vertOut vert (vertIn input) {
                vertOut output;

                output.position = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.vertex;

                return output;
            }

            samplerCUBE _MainTex;

            fixed4 frag(vertOut input) : SV_Target{
                float3 normal = input.texcoord * 2.0f;

                normal = sign(normal) * smoothstep(0.75, 1.0, abs(normal));

                float3 uv = float3(input.texcoord.x * -1, input.texcoord.yz);              
                float3 font = texCUBE(_MainTex, uv).xyz;

                float diff = max(0, dot(normalize(float3(1, 1, -1)), normal));

                return float4(font + diff * 0.1, 0);
            }
            ENDCG
        }
    }
}
