Shader "Hidden/PurrUI/RectangleRenderer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // Sprite mode only (PURR_SPRITE). Declared here so material copies made for masking keep them.
        [HideInInspector] _SpriteOuterUV ("Sprite Outer UV", Vector) = (0,0,1,1)
        [HideInInspector] _SpriteInnerUV ("Sprite Inner UV", Vector) = (0,0,1,1)
        [HideInInspector] _SpriteBorder ("Sprite Border", Vector) = (0,0,0,0)
        [HideInInspector] _SpritePadding ("Sprite Padding", Vector) = (0,0,0,0)
        [HideInInspector] _SpritePaddingRelative ("Sprite Padding Relative", Float) = 0
        [HideInInspector] _SpriteFillCenter ("Sprite Fill Center", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            // multi_compile, not shader_feature: the sprite materials are created at runtime, so a
            // shader_feature variant would be stripped from player builds.
            #pragma multi_compile_local _ PURR_SPRITE

            // Vertex attributes packed by OnPopulateMesh:
            //   uv0: texU, texV, width, height
            //   uv1: roundness (x, y, z, w)
            //   uv2: encodedOutline, shadowSize, shadowBlur, encodedShadowPow
            //        encodedOutline: >= 0 → outlineSize; < 0 → frame mode, abs = 1 + placement*4096 + outlineSize
            //        encodedShadowPow: shadowPow + round(frameWidth)*256
            //   uv3: packedOutlineColor (xy), packedShadowColor (zw)
            //   color: graphicColor * Graphic.color
            // Sprite mode keeps this layout and reads the sprite's atlas rect and borders from the
            // material instead, so the plain path and its vertex data are untouched.

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float4 color    : COLOR;
                float4 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                float4 uv3      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float4 texAndSdf    : TEXCOORD0; // xy=texcoord, zw=sdfPosition
                float4 roundness    : TEXCOORD1;
                float4 params       : TEXCOORD2; // xy=halfSize, z=outlineSize, w=shadowSize
                float4 params2      : TEXCOORD3; // x=shadowBlur, y=shadowPow, zw=worldPos
                half4 outlineColor  : TEXCOORD4;
                half4 shadowColor   : TEXCOORD5;
                float3 emboss       : TEXCOORD6; // x=size, y=angle, z=strength
                half4 embossHColor  : TEXCOORD7;
                half4 embossLColor  : TEXCOORD8;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            // Unpack a Color32 from 2 floats (r+g*256, b+a*256)
            half4 UnpackColor(float2 packed)
            {
                half g = floor(packed.x / 256.0);
                half r = packed.x - g * 256.0;
                half a = floor(packed.y / 256.0);
                half b = packed.y - a * 256.0;
                return half4(r, g, b, a) / 255.0;
            }

            v2f vert(appdata v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.color = v.color * _Color;

                // UV transform for padding
                float2 size = v.uv0.zw;
                float rawOutline = v.uv2.x;
                float outlineExtent;
                if (rawOutline < -0.5)
                {
                    float absVal = abs(rawOutline) - 1.0;
                    float placement = floor(absVal / 4096.0 + 0.001);
                    float olSize = absVal - placement * 4096.0;
                    float fw = floor(v.uv2.w / 256.0 + 0.001);
                    float outerEdge = placement < 0.5 ? 0.0
                                    : placement < 1.5 ? fw * 0.5
                                    : fw;
                    outlineExtent = olSize + outerEdge;
                }
                else
                {
                    outlineExtent = rawOutline;
                }
                float padding = outlineExtent + v.uv2.y + v.uv2.z + 1.0; // outline+frame + shadow + blur + 1px
                float2 normPad = padding / size;
                float2 uv = v.uv0.xy * (1 + normPad * 2) - normPad;

                OUT.texAndSdf = float4(uv, (uv - 0.5) * size);
                OUT.roundness = v.uv1;
                OUT.params = float4(size * 0.5, v.uv2.xy);
                OUT.params2 = float4(v.uv2.zw, v.vertex.xy);

                // Unpack colors in vertex shader (4 verts) instead of fragment (thousands)
                OUT.outlineColor = UnpackColor(v.uv3.xy);
                OUT.shadowColor = UnpackColor(v.uv3.zw);
                OUT.emboss = v.normal;
                OUT.embossHColor = UnpackColor(v.tangent.xy);
                OUT.embossLColor = UnpackColor(v.tangent.zw);

                return OUT;
            }

            float sdRoundedBox(float2 p, float2 b, float4 r)
            {
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x = (p.y > 0.0) ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            #ifdef PURR_SPRITE
            float4 _SpriteOuterUV;      // atlas rect of the sprite (xMin, yMin, xMax, yMax)
            float4 _SpriteInnerUV;      // atlas rect of the stretched centre
            float4 _SpriteBorder;       // left, bottom, right, top in rect pixels
            float4 _SpritePadding;      // trimmed transparent edge: rect pixels, or a fraction of the rect
            float _SpritePaddingRelative;
            float _SpriteFillCenter;    // 0 leaves the centre of a nine-sliced sprite empty

            // Maps a position in the rect to the sprite's atlas UV, stretching only the centre of a
            // nine-sliced sprite. mask is 0 over the sprite's trimmed transparent padding.
            float2 SpriteUV(float2 rectUV, float2 size, out float mask)
            {
                float2 p = rectUV * size;

                float4 pad = lerp(_SpritePadding, _SpritePadding * size.xyxy, _SpritePaddingRelative);
                float2 lo = pad.xy;
                float2 hi = size - pad.zw;

                // Borders that do not fit the rect shrink evenly, like UI.Image does.
                float4 border = _SpriteBorder;
                float2 borderScale = min(1.0, size / max(border.xy + border.zw, 0.0001));
                border *= borderScale.xyxy;

                float2 b0 = clamp(border.xy, lo, hi);
                float2 b1 = clamp(size - border.zw, b0, hi);

                float2 uvA = lerp(_SpriteOuterUV.xy, _SpriteInnerUV.xy, saturate((p - lo) / max(b0 - lo, 0.0001)));
                float2 uvB = lerp(_SpriteInnerUV.xy, _SpriteInnerUV.zw, saturate((p - b0) / max(b1 - b0, 0.0001)));
                float2 uvC = lerp(_SpriteInnerUV.zw, _SpriteOuterUV.zw, saturate((p - b1) / max(hi - b1, 0.0001)));

                // Without padding the texture clamps past the rect edge, so the antialiased edge and
                // an outside frame keep their colour.
                float2 inLo = max(step(lo, p), step(pad.xy, 0.0));
                float2 inHi = max(step(p, hi), step(pad.zw, 0.0));
                mask = inLo.x * inLo.y * inHi.x * inHi.y;

                float2 inCentre = step(b0, p) * step(p, b1);
                mask *= 1.0 - inCentre.x * inCentre.y * (1.0 - _SpriteFillCenter);

                return lerp(uvA, lerp(uvB, uvC, step(b1, p)), step(b0, p));
            }
            #endif

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfSize = IN.params.xy;
                float rawOutline = IN.params.z;
                float shadowSize = IN.params.w;
                float shadowBlur = IN.params2.x;

                // Decode frame mode from encoded outline (negative = frame on)
                bool frameMode = rawOutline < -0.5;
                float outlineSize, outerEdge, innerEdge;
                float shadowPow;

                if (frameMode)
                {
                    float absVal = abs(rawOutline) - 1.0;
                    float placement = floor(absVal / 4096.0 + 0.001);
                    outlineSize = absVal - placement * 4096.0;

                    float fw = floor(IN.params2.y / 256.0 + 0.001);
                    shadowPow = IN.params2.y - fw * 256.0;

                    // Compute edges from placement and frameWidth
                    if (placement < 0.5)       { outerEdge = 0.0;      innerEdge = -fw; }      // Inside
                    else if (placement < 1.5)  { outerEdge = fw * 0.5; innerEdge = -fw * 0.5; } // Center
                    else                       { outerEdge = fw;       innerEdge = 0.0; }       // Outside
                }
                else
                {
                    outlineSize = rawOutline;
                    outerEdge = 0;
                    innerEdge = 0;
                    shadowPow = IN.params2.y;
                }

                #ifdef PURR_SPRITE
                float spriteMask;
                float2 spriteUV = SpriteUV(IN.texAndSdf.xy, halfSize * 2.0, spriteMask);
                half4 graphic = tex2D(_MainTex, spriteUV) + _TextureSampleAdd;
                graphic.a *= spriteMask;
                #else
                half4 graphic = tex2D(_MainTex, IN.texAndSdf.xy) + _TextureSampleAdd;
                #endif

                float dist = sdRoundedBox(IN.texAndSdf.zw, halfSize, IN.roundness);

                // In frame mode, derive a ring SDF from the shape SDF.
                // ring_sdf < 0 inside the ring wall, > 0 in the hole and outside.
                // All effects then naturally wrap both inner and outer edges.
                float ringCenter = 0;
                float ringHalf = 0;
                float sdf;
                if (frameMode)
                {
                    ringCenter = (outerEdge + innerEdge) * 0.5;
                    ringHalf = (outerEdge - innerEdge) * 0.5;
                    sdf = abs(dist - ringCenter) - ringHalf;
                }
                else
                {
                    sdf = dist;
                }

                float delta = fwidth(sdf);

                // Fill
                float fill = 1 - smoothstep(0, delta, sdf);

                // Outline: band just outside the ring surface
                float outline = outlineSize > 0
                    ? 1 - smoothstep(outlineSize, outlineSize + delta, sdf)
                    : 0;

                // Shadow: beyond outline
                float shadow = 0;
                float shadowRange = shadowSize + shadowBlur;
                if (shadowRange > 0)
                {
                    float shadowEdge = outlineSize + shadowSize;
                    shadow = 1 - smoothstep(
                        shadowEdge - shadowBlur,
                        shadowEdge + delta,
                        sdf);
                    shadow = pow(shadow, shadowPow);
                }

                // Emboss: offset SDF samples along light direction for 3D bevel
                half3 fillRgb = graphic.rgb * IN.color.rgb;

                float embossSize = IN.emboss.x;
                if (embossSize > 0)
                {
                    float2 lightDir = float2(cos(IN.emboss.y), sin(IN.emboss.y));
                    float2 offset = lightDir * embossSize;

                    float dA = sdRoundedBox(IN.texAndSdf.zw + offset, halfSize, IN.roundness);
                    float dB = sdRoundedBox(IN.texAndSdf.zw - offset, halfSize, IN.roundness);

                    // Apply ring transform to offset samples too
                    if (frameMode)
                    {
                        dA = abs(dA - ringCenter) - ringHalf;
                        dB = abs(dB - ringCenter) - ringHalf;
                    }

                    // Directional derivative ≈ dot(surfaceNormal, lightDir)
                    float light = clamp((dA - dB) / (embossSize * 2.0), -1.0, 1.0);
                    light = sign(light) * pow(abs(light), 1.0 + (1.0 - IN.emboss.z) * 4.0);

                    // Band mask: emboss only near the edge, fades inward
                    float bandMask = smoothstep(-embossSize, 0, sdf);

                    // Lerp toward highlight or shadow tint
                    half3 tint = light > 0 ? IN.embossHColor.rgb : IN.embossLColor.rgb;
                    fillRgb = lerp(fillRgb, tint, abs(light) * bandMask * saturate(fill * 8));
                }

                // vertex.color = graphicColor * Graphic.color
                // .a is master alpha (CanvasGroup compatible)
                float masterAlpha = IN.color.a;
                float gA = fill * graphic.a * masterAlpha;
                float oA = outline * IN.outlineColor.a * masterAlpha;
                float sA = shadow * IN.shadowColor.a * masterAlpha;

                // Inlined Over(fill, Over(outline, shadow)) in premultiplied space
                float oneMinusGA = 1 - gA;
                float oneMinusOA = 1 - oA;

                half4 result;
                result.rgb = fillRgb * gA
                    + IN.outlineColor.rgb * oA * oneMinusGA
                    + IN.shadowColor.rgb * sA * oneMinusOA * oneMinusGA;
                result.a = gA
                    + oA * oneMinusGA
                    + sA * oneMinusOA * oneMinusGA;

                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(IN.params2.zw, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
