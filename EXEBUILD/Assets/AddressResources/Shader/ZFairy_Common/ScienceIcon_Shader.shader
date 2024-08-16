Shader "Custom/ScienceIcon_Shader"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
        [HDR]_TintColor("Tint Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        LOD 200

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        
        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            Fog { Mode Off }
            Offset -1, -1
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag            
            #include "UnityCG.cginc"

            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _TintColor;
            CBUFFER_END

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
    
            struct v2f
            {
                float4 vertex : SV_POSITION;
                half2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
    
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }
                
            fixed4 frag(v2f IN) : COLOR
            {
                // 从纹理采样颜色
                fixed4 col = tex2D(_MainTex, IN.texcoord);

                // 计算颜色接近黑色的程度
                float grayscale = dot(col.rgb, float3(0.3, 0.59, 0.11));

                // 根据接近黑色的程度设置透明度（越接近黑色，alpha越小，接近白色的部分保留透明度）
                col.a = saturate(grayscale);

                // 将颜色与Tint颜色混合 (使用HDR)
                col.rgb *= _TintColor.rgb;

                return col;
            }
            ENDCG
        }
    }
}
