Shader "Game/PlayerHairColor"
{
    Properties
    {
        [PerRendererData] _MainTex("Diffuse",2D)="white"{}
        _MaskTex("Mask",2D)="white"{}
        _NormalMap("Normal Map",2D)="bump"{}
        _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("Renderer Color",Color)=(1,1,1,1)
        [PerRendererData] _HairRect("Hair UV bounds",Vector)=(0,0,0,0)
        _HairShadow("Hair shadow",Color)=(0.19,0.025,0.018,1)
        _HairHighlight("Hair highlight",Color)=(1,0.31,0.18,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags {"LightMode"="Universal2D"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            struct Attributes {float3 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;half2 lightingUV:TEXCOORD1;};
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            half4 _Color,_RendererColor,_HairShadow,_HairHighlight;
            float4 _HairRect;
            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS=TransformObjectToHClip(v.positionOS);
                o.uv=v.uv;
                o.lightingUV=half2(ComputeScreenPos(o.positionCS/o.positionCS.w).xy);
                o.color=v.color*_Color*_RendererColor;
                return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half lo=min(c.r,min(c.g,c.b)), hi=max(c.r,max(c.g,c.b));
                half inside=step(_HairRect.x,i.uv.x)*step(_HairRect.y,i.uv.y)*step(i.uv.x,_HairRect.z)*step(i.uv.y,_HairRect.w);
                half neutral=1-smoothstep(.10,.19,hi-lo);
                half lightness=smoothstep(.10,.23,hi);
                half shade=dot(c.rgb,half3(.2126,.7152,.0722));
                c.rgb=lerp(c.rgb,lerp(_HairShadow.rgb,_HairHighlight.rgb,saturate(shade)),inside*neutral*lightness);
                c*=i.color;
                SurfaceData2D surface;InputData2D input;
                InitializeSurfaceData(c.rgb,c.a,SAMPLE_TEXTURE2D(_MaskTex,sampler_MaskTex,i.uv),surface);
                InitializeInputData(i.uv,i.lightingUV,input);
                return CombinedShapeLightShared(surface,input);
            }
            ENDHLSL
        }
        Pass
        {
            Tags {"LightMode"="NormalsRendering"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float3 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 tangent:TANGENT;};
            struct Varyings {float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;half3 normalWS:TEXCOORD1;half3 tangentWS:TEXCOORD2;half3 bitangentWS:TEXCOORD3;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS=TransformObjectToHClip(v.positionOS);o.uv=v.uv;o.color=v.color;
                o.normalWS=-GetViewForwardDir();o.tangentWS=TransformObjectToWorldDir(v.tangent.xyz);
                o.bitangentWS=cross(o.normalWS,o.tangentWS)*v.tangent.w;return o;
            }
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
            half4 frag(Varyings i):SV_Target
            {
                half4 c=i.color*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half3 n=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
                return NormalsRenderingShared(c,n,i.tangentWS,i.bitangentWS,i.normalWS);
            }
            ENDHLSL
        }
    }
}
