Shader "XXY/UI_MainIco"
{
    Properties
    {
        _MainTex("Base (RGB), Alpha (A)", 2D) = "black" {}
        [HDR]_MainColor("_MainColor", COLOR) = (1,1,1,1)
        _ColorMul("乘颜色",float) = 1
        
//         _IsResearching("是否正在研究中",int) = 0
        [Toggle]_ISRESEACH("是否正在研究中",int) = 0
    }

    SubShader
    {
        LOD 200

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" ="UniversalPipeline"
        }

        Pass
        {
            Cull Off
            Lighting Off
            ZWrite Off
            Offset -1, -1
            Fog
            {
                Mode Off
            }
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ISRESEACH_ON

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            
            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _ClipRange0 = float4(0.0, 0.0, 1.0, 1.0);
            float2 _ClipArgs0 = float2(1000.0, 1000.0);
            half4 _MainColor;
            float _IsResearching,_ColorMul;
            CBUFFER_END

            struct appdata_t
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            v2f o;

            v2f vert(appdata_t v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = TransformObjectToHClip(v.vertex);
                o.color = v.color;
                o.texcoord = v.texcoord;

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = (worldPos.xy) * _ClipRange0.zw + _ClipRange0.xy;
                return o;
            }

            half4 frag(v2f IN) : SV_Target
            {
                // Softness factor
                float2 factor = (float2(1.0, 1.0) - abs(IN.worldPos)) * float2(1000, 1000); // * _ClipArgs0;

                // Sample the texture
               
                half alpha = clamp(min(factor.x, factor.y), 0.0, 1.0);
                half4 var_MainTex = tex2D(_MainTex, IN.texcoord);
                 half4 col =var_MainTex *var_MainTex.r * IN.color * _MainColor*_ColorMul*alpha;

                 
                #if _ISRESEACH_ON
                 col = col*(sin(_Time.y*2)+1.2)*0.7;
                #endif
                
              
                //  if(_IsResearching == 1)
                // {
                //     col = col*(sin(_Time.y*10)+1.3);
                // }
                  return col;
            }
            ENDHLSL
        }
    }

}