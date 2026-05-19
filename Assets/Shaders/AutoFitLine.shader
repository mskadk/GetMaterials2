Shader "Custom/AutoFitLine"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Width ("Width", Float) = 5.0
        _MinPixels ("Min Pixels", Float) = 2.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            #include "UnityCG.cginc"
            
            float4 _Color;
            float _Width;
            float _MinPixels;
            
            struct v2g
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
            
            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };
            
            v2g vert(appdata_full v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.color = v.color;
                return o;
            }
            
            [maxvertexcount(6)]
            void geom(line v2g input[2], inout TriangleStream<g2f> triStream)
            {
                float4 p0 = UnityObjectToClipPos(input[0].vertex);
                float4 p1 = UnityObjectToClipPos(input[1].vertex);
                
                float2 ndc0 = p0.xy / p0.w;
                float2 ndc1 = p1.xy / p1.w;
                
                float2 lineDir = ndc1 - ndc0;
                float2 perpDir = normalize(float2(-lineDir.y, lineDir.x));
                
                // ÏñËØ¿í¶È×ªNDC
                float width = max(_Width, _MinPixels) / _ScreenParams.y * 2.0;
                float2 offset = perpDir * width;
                
                g2f o;
                o.color = input[0].color * _Color;
                
                o.pos = p0;
                o.pos.xy += offset * p0.w;
                triStream.Append(o);
                
                o.pos = p0;
                o.pos.xy -= offset * p0.w;
                triStream.Append(o);
                
                o.color = input[1].color * _Color;
                
                o.pos = p1;
                o.pos.xy += offset * p1.w;
                triStream.Append(o);
                
                triStream.RestartStrip();
                
                o.pos = p0;
                o.pos.xy -= offset * p0.w;
                o.color = input[0].color * _Color;
                triStream.Append(o);
                
                o.pos = p1;
                o.pos.xy -= offset * p1.w;
                o.color = input[1].color * _Color;
                triStream.Append(o);
                
                o.pos = p1;
                o.pos.xy += offset * p1.w;
                triStream.Append(o);
            }
            
            float4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}