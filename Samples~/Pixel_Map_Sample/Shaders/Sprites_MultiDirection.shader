// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// Modified version to support billboard based multi direction sprite

Shader "Sprites/MultiDirection" {
    Properties {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
        _Settings ("xOffset | yOffset | DirectionCount", Vector) = (0.125, 0.0, 8, 0)
    }

        SubShader {
            Tags {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
                "CanUseSpriteAtlas" = "True"
                "DisableBatching" = "True"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Blend One OneMinusSrcAlpha

            Pass {
            CGPROGRAM
                #pragma vertex SpriteVert
                #pragma fragment SpriteFrag
                #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma multi_compile_local _ PIXELSNAP_ON
                #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
                
            #include "UnityCG.cginc"

            #ifdef UNITY_INSTANCING_ENABLED

            UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
                    // SpriteRenderer.Color while Non-Batched/Instanced.
                    UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
                    // this could be smaller but that's how bit each entry is regardless of type
                    UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
                UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

                #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
                #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

            #endif // instancing

            CBUFFER_START(UnityPerDrawSprite)
            #ifndef UNITY_INSTANCING_ENABLED
                fixed4 _RendererColor;
                fixed2 _Flip;
            #endif
                float _EnableExternalAlpha;
            CBUFFER_END

                fixed4 _Color;
                float3 _Settings;

                struct appdata_t {
                    float4 vertex   : POSITION;
                    float4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex   : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                inline float4 UnityFlipSprite(in float3 pos, in fixed2 flip) {
                    return float4(pos.xy * flip, pos.z, 1.0);
                }

                float3 rotatePoint(float3 v, float angle) {
                    return float3(
                        v.x * cos(angle) - v.z * sin(angle),
                        v.y,
                        v.x * sin(angle) + v.z * cos(angle)
                    );
                }

                v2f SpriteVert(appdata_t IN) {
                    v2f OUT;

                    UNITY_SETUP_INSTANCE_ID(IN);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                    // Insert get billboard and sprite rotation from camera
                    float PI = 3.14159265359;
                    float PI2 = PI * 2;

                    // Use float4(UNITY_MATRIX_IT_MV[2].xyz, 0) instead in matrix calculation to ignore relative placement
                    float3 objSpaceCam = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
                    float3 toCam = normalize(-objSpaceCam);
                    float rot2D = atan2(toCam.x, -toCam.z);

                    float frame = round((rot2D / PI2) * _Settings.z);

                    OUT.vertex = IN.vertex;
                    OUT.vertex.xyz = rotatePoint(OUT.vertex.xyz, rot2D);
                    OUT.vertex = UnityFlipSprite(OUT.vertex, _Flip);
                    OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                    OUT.texcoord = IN.texcoord + frac(frame * _Settings.xy);

                    OUT.color = IN.color * _Color * _RendererColor;

                    #ifdef PIXELSNAP_ON
                    OUT.vertex = UnityPixelSnap(OUT.vertex);
                    #endif

                    return OUT;
                }

                sampler2D _MainTex;
                sampler2D _AlphaTex;

                fixed4 SampleSpriteTexture(float2 uv)
                {
                    fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                    fixed4 alpha = tex2D(_AlphaTex, uv);
                    color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif

                    return color;
                }

                fixed4 SpriteFrag(v2f IN) : SV_Target
                {
                    fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                    c.rgb *= c.a;
                    return c;
                }

            ENDCG
            }
        }
}