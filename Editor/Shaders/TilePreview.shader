// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

Shader "Hidden/TilePreview" {
    Properties {
        _Color ("Color", Color) = (0.5, 0.5, 0.5, 0.25)
        _Grid ("Grid", Vector) = (0, 0, 1, 1)
        _Cursor ("Cursor", Vector) = (0, 0, 1, 1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        ZWrite On
        Offset -1, -1

        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertIn {
                float4 vertex : POSITION;
            };

            struct vertOut {
                float4 position : SV_POSITION;
            };

            vertOut vert (vertIn input) {
                vertOut output;

                output.position = UnityObjectToClipPos(input.vertex);

                return output;
            }

            float4 _Color;

            fixed4 frag (vertOut input) : SV_Target {
                return _Color;
            }
            ENDCG
        }

        Pass {
            Cull Off
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertIn {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct vertOut {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 depth : TEXCOORD1;
            };

            float4 _Grid;
            float4 _Cursor;

            vertOut vert(vertIn input) {
                vertOut output;

                output.position = UnityObjectToClipPos(input.vertex);

                output.texcoord = input.texcoord.xy;
                output.depth = output.position.zw;

                return output;
            }

            float MaskArea(float2 texcoord, float2 tMin, float2 tMax) {
                float2 mask = saturate((texcoord - (tMin - 1))) * (1 - saturate((texcoord - tMax)));                

                return min(mask.x, mask.y);
            }

            fixed4 frag(vertOut input) : SV_Target{
                float2 uv = frac(input.texcoord);

                float depth = LinearEyeDepth(input.depth.x / input.depth.y);

                float2 outlineWidth = depth * 0.008;
                outlineWidth /= _Grid.zw;

                float2 mirrored = abs(uv - 0.5) * 2;
                mirrored = smoothstep(1.0 - outlineWidth, 1.0, mirrored);

                float cursorMask = MaskArea(input.texcoord, _Cursor.xy, _Cursor.zw);
                cursorMask *= cursorMask;

                float border = max(mirrored.x, mirrored.y);
                float mask = MaskArea(input.texcoord, 0, _Grid.xy);
                border *= smoothstep(0.5, 1.0, mask);

                float3 color = lerp(float3(0.5, 0.5, 0.5), float3(1, 0.5, 0), cursorMask);

                return float4(color, border * 0.25);
            }
            ENDCG
        }

        Pass{
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct vertIn {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct vertOut {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };

            vertOut vert(vertIn input) {
                vertOut output;

                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color;

                return output;
            }

            fixed4 frag(vertOut input) : SV_Target{
                return input.color;
            }
            ENDCG
        }

    }
}
