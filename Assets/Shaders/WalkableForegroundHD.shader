Shader "Yongshi/Walkable Foreground HD"
{
    Properties
    {
        _MainTex("Detailed Quarter",2D)="white"{}
        _Brightness("Foreground Brightness",Range(.5,1.5))=1.06
        _RimStrength("Subtle Walkable Rim",Range(0,1))=.18
        _EndFeather("Exposed Platform End Blend",Range(.1,1.5))=.85
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a {float4 vertex:POSITION;float2 uv:TEXCOORD0;float2 depth:TEXCOORD1;float2 ends:TEXCOORD2;};
            struct v {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float2 depth:TEXCOORD1;float2 ends:TEXCOORD2;};
            sampler2D _MainTex;float _Brightness,_RimStrength,_EndFeather;
            v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.depth=i.depth;o.ends=i.ends;return o;}
            fixed4 frag(v i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                float bottom=1-smoothstep(.60,1,i.depth.y);
                // Blend segment boundaries back into the original panorama.
                float sides=smoothstep(0,.012,i.uv.x)*(1-smoothstep(.988,1,i.uv.x));
                float top=smoothstep(-.65,-.18,i.depth.x);
                float rim=1-smoothstep(.012,.085,abs(i.depth.x));
                // Texture-quarter boundaries and physical platform ends are different.
                // Keep a narrow walking rim while blending hanging faces more broadly.
                float endWidth=lerp(.20,_EndFeather,saturate(i.depth.y*2));
                float platformEnds=smoothstep(0,endWidth,min(i.ends.x,i.ends.y));
                c.rgb=c.rgb*_Brightness+float3(.11,.078,.035)*rim*_RimStrength;
                c.a*=top*bottom*sides*platformEnds;
                return c;
            }
            ENDHLSL
        }
    }
}
