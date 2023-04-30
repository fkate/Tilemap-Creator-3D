// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// A custom skybox shader for displaying a 2D parralax effect

Shader "Unlit/Unlit_ParallaxScrolling"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Layer0 ("Layer 0", Color) = (1, 1, 1, 1)
        _Layer1 ("Layer 1", Color) = (0.5, 0.5, 0.5, 1)
        _Layer2 ("Layer 2", Color) = (0.25, 0.25, 0.25, 1)
        _Layer3 ("Layer 3", Color) = (0, 0, 0, 1)
        _Depth ("Layer Depth", Vector) = (0, 0.25, 0.5, 0.0)
        _Scroll ("Layer Scroll", Vector) = (0.5, 0.0, 0.0, 0.0)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderType" = "Background" "PreviewType" = "Skybox" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 camDir : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed3 _Layer0;
            fixed3 _Layer1;
            fixed3 _Layer2;
            fixed3 _Layer3;
            fixed3 _Depth;
            fixed2 _Scroll;

            v2f vert (appdata v) {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.camDir = unity_CameraToWorld._m02_m12_m22.xy;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                float aspect = _ScreenParams.x / _ScreenParams.y;

                float2 cameraPos = _WorldSpaceCameraPos.xy;
                float2 baseLayerPos = (i.screenPos.xy / i.screenPos.w);
                baseLayerPos.x *= aspect;

                float2 layer0Pos = baseLayerPos + i.camDir * _Depth.x;
                float2 layer1Pos = baseLayerPos + i.camDir * _Depth.y;
                float2 layer2Pos = baseLayerPos + i.camDir * _Depth.z;

                layer0Pos += cameraPos.xy * _Scroll * _Depth.x;
                layer1Pos += cameraPos.xy * _Scroll * _Depth.y;
                layer2Pos += cameraPos.xy * _Scroll * _Depth.z;

                fixed3 mask;
                mask.x = tex2D(_MainTex, layer0Pos).r;
                mask.y = tex2D(_MainTex, layer1Pos).g;
                mask.z = tex2D(_MainTex, layer2Pos).b;

                fixed3 c = lerp(_Layer3, _Layer2, mask.z);
                c = lerp(c, _Layer1, mask.y);
                c = lerp(c, _Layer0, mask.x);

                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
