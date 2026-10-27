Shader "Yongshi/Walkable Foreground Ladder Repair"
{
    Properties
    {
        _MainTex("Detailed Quarter",2D)="white"{}
        _CleanTex("Ladder-free Panorama",2D)="white"{}
        _CleanUV("Quarter to Panorama UV",Vector)=(1,0,0,0)
        _Repair0("Ladder Region 0",Vector)=(0,0,0,0)
        _Repair1("Ladder Region 1",Vector)=(0,0,0,0)
        _Repair2("Ladder Region 2",Vector)=(0,0,0,0)
        _Repair3("Ladder Region 3",Vector)=(0,0,0,0)
        _Repair4("Ladder Region 4",Vector)=(0,0,0,0)
        _Repair5("Ladder Region 5",Vector)=(0,0,0,0)
        _Repair6("Ladder Region 6",Vector)=(0,0,0,0)
        _Repair7("Ladder Region 7",Vector)=(0,0,0,0)
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
            sampler2D _CleanTex;
            float4 _CleanUV,_Repair0,_Repair1,_Repair2,_Repair3,_Repair4,_Repair5,_Repair6,_Repair7;
            float repairMask(float2 uv,float4 rect)
            {
                float2 f=float2(4.0/2048.0,4.0/683.0);
                return step(.00001,rect.z-rect.x)*smoothstep(rect.x,rect.x+f.x,uv.x)*(1-smoothstep(rect.z-f.x,rect.z,uv.x))*smoothstep(rect.y,rect.y+f.y,uv.y)*(1-smoothstep(rect.w-f.y,rect.w,uv.y));
            }
            v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.depth=i.depth;o.ends=i.ends;return o;}
            fixed4 frag(v i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                float2 globalUV=float2(i.uv.x*_CleanUV.x+_CleanUV.y,i.uv.y);
                float mask=max(max(repairMask(globalUV,_Repair0),repairMask(globalUV,_Repair1)),max(repairMask(globalUV,_Repair2),repairMask(globalUV,_Repair3)));
                mask=max(mask,max(max(repairMask(globalUV,_Repair4),repairMask(globalUV,_Repair5)),max(repairMask(globalUV,_Repair6),repairMask(globalUV,_Repair7))));
                c=lerp(c,tex2D(_CleanTex,globalUV),mask);
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

