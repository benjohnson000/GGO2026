// Unlit sprite shader for tiles: draws the sprite tinted by the renderer colour, plus a pixel outline
// around its opaque pixels. Outline colour and thickness are set per renderer by TileView.
// Needs the sprite imported with Mesh Type: Full Rect and transparent margin of at least the outline thickness,
// otherwise the outline is clipped by the sprite mesh or picks up neighbouring atlas slices.
//
// Optional shape mask (used by patterns): when _MaskRect has a size, the sprite is multiplied by the alpha of
// another sprite (_MaskTex / _MaskRect), aligned centre to centre texel for texel. Each renderer masks only
// itself, unlike SpriteMask, whose stencil would also cut into neighbouring tiles while they overlap.
Shader "GGO2026/Tile Outline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0.18, 0.133, 0.184, 1)
        _OutlineThickness ("Outline Thickness (pixels)", Range(0, 3)) = 2

        // Shape mask, set per renderer by TileView. Rects are in texels: (x, y, width, height).
        [HideInInspector] _MaskTex ("Mask Texture", 2D) = "white" {}
        [HideInInspector] _MaskRect ("Mask Rect", Vector) = (0, 0, 0, 0)
        [HideInInspector] _SpriteRect ("Sprite Rect", Vector) = (0, 0, 0, 0)

        // Legacy properties, kept so the material falls back gracefully like the built-in sprite shaders.
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            // Same layout for every variant so the SRP batcher stays compatible.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineThickness;
                float4 _MaskRect;
                float4 _SpriteRect;
            CBUFFER_END

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // Alpha of the mask sprite under this texel, or 1 when no mask is set.
            half SampleMask(float2 uv)
            {
                if (_MaskRect.z <= 0) return 1;

                float mainWidth, mainHeight, maskWidth, maskHeight;
                _MainTex.GetDimensions(mainWidth, mainHeight);
                _MaskTex.GetDimensions(maskWidth, maskHeight);

                // Texel position inside this sprite's rect, then inside the mask's rect (centres aligned).
                float2 spriteTexel = uv * float2(mainWidth, mainHeight) - _SpriteRect.xy;
                float2 maskTexel = spriteTexel - (_SpriteRect.zw - _MaskRect.zw) * 0.5;
                if (any(maskTexel < 0) || any(maskTexel >= _MaskRect.zw)) return 0;

                float2 maskUV = (_MaskRect.xy + maskTexel) / float2(maskWidth, maskHeight);
                return SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, maskUV).a;
            }

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                sprite.a *= SampleMask(input.uv);
                half4 fill = sprite * input.color;
                if (sprite.a > 0.5h || _OutlineThickness <= 0) return fill;

                // Any opaque texel within a rounded radius of this one makes it an outline texel.
                // The +1 keeps a 2px outline round: includes (2,1) but not the (2,2) corner.
                float limit = _OutlineThickness * _OutlineThickness + 1.0;

                // Read the size from the bound texture: the sprite texture is bound per renderer,
                // so a _MainTex_TexelSize in the material cbuffer would describe the material's texture instead.
                float textureWidth, textureHeight;
                _MainTex.GetDimensions(textureWidth, textureHeight);
                float2 texel = 1.0 / float2(textureWidth, textureHeight);

                half coverage = 0;
                [unroll] for (int y = -3; y <= 3; y++)
                {
                    [unroll] for (int x = -3; x <= 3; x++)
                    {
                        if (x * x + y * y <= limit)
                        {
                            float2 offset = float2(x, y) * texel;
                            coverage = max(coverage, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset).a);
                        }
                    }
                }

                half4 outline = _OutlineColor;
                outline.a *= step(0.5h, coverage) * input.color.a;
                return outline;
            }
            ENDHLSL
        }
    }
}
