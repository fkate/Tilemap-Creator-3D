// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit alpha-cutout shader.
// - no lighting
// - no lightmap support
// - no per-material color

// Edited version to include uv offset animation

Shader "Unlit/Transparent Cutout Animated" {
    Properties{
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
        _Parameters ("AnimOffset (XY) Frames (Z) Speed (W)", Vector) = (0.25, 0.0, 4.0, 1.0)
    }
        SubShader{
            Tags {"Queue" = "AlphaTest" "IgnoreProjector" = "true" "RenderType" = "TransparentCutout"}
            LOD 100

            Lighting Off

            Pass {
                CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #pragma target 2.0
                    #pragma multi_compile_fog

                    #include "UnityCG.cginc"

                    struct appdata_t {
                        float4 vertex : POSITION;
                        float2 texcoord : TEXCOORD0;
                        UNITY_VERTEX_INPUT_INSTANCE_ID
                    };

                    struct v2f {
                        float4 vertex : SV_POSITION;
                        float2 texcoord : TEXCOORD0;
                        UNITY_FOG_COORDS(1)
                        UNITY_VERTEX_OUTPUT_STEREO
                    };

                    sampler2D _MainTex;
                    float4 _MainTex_ST;
                    fixed _Cutoff;
                    fixed4 _Parameters;

                    v2f vert(appdata_t v) {
                        v2f o;
                        UNITY_SETUP_INSTANCE_ID(v);
                        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                        float frame = floor(_Time.y * _Parameters.w % _Parameters.z);

                        o.texcoord += frame * _Parameters.xy;

                        UNITY_TRANSFER_FOG(o,o.vertex);
                        return o;
                    }

                    fixed4 frag(v2f i) : SV_Target {
                        fixed4 col = tex2D(_MainTex, i.texcoord);
                        clip(col.a - _Cutoff);
                        UNITY_APPLY_FOG(i.fogCoord, col);
                        return col;
                    }
                ENDCG
            }
        }

}