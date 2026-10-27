Shader "Yongshi/Mine Panorama Water"
{
    Properties { [PerRendererData] _MainTex("Panorama",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct a { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; fixed4 _Color;
            v vert(a i) { v o; o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color*_Color;return o; }
            fixed4 frag(v i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                float blue=saturate((c.b-c.r)*5)*saturate((c.g-c.r)*5);
                float region=smoothstep(.54,.57,i.uv.x)*(1-smoothstep(.69,.73,i.uv.x));
                float water=blue*region;
                float2 uv=i.uv;
                uv.x+=sin(i.uv.y*240+_Time.y*3.2)*.00065*water;
                uv.y+=sin(i.uv.y*180+_Time.y*4.5)*.0011*water;
                c=tex2D(_MainTex,uv);
                c.rgb*=1+sin(i.uv.y*200+_Time.y*5)*.035*water;
                return c*i.color;
            }
            ENDHLSL
        }
    }
}
