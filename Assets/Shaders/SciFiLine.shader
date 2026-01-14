Shader "Custom/SciFi_ProceduralLine"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Texture (Optional)", 2D) = "white" {} // 即使不用贴图也要保留占位
        _CoreWidth ("Core Width (中心宽度)", Range(0.1, 20.0)) = 3.0
        _Hardness ("Edge Hardness (边缘硬度)", Range(1.0, 10.0)) = 2.0
        
        [Header(Animation)]
        _FlowSpeed ("Flow Speed (流动速度)", Range(-10.0, 10.0)) = 2.0
        _FlowIntensity ("Flow Intensity (流动亮度增强)", Range(0.0, 1.0)) = 0.3
        _FlowFrequency ("Flow Frequency (流动密度)", Range(1.0, 50.0)) = 10.0
        
        [Header(Glow)]
        _EmissionGain ("Emission Gain (发光强度)", Range(1.0, 5.0)) = 1.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "PreviewType"="Plane"
        }
        
        //以此混合模式实现发光叠加效果（背景越黑越亮）
        Blend SrcAlpha One 
        ZWrite Off
        Cull Off 
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // 获取LineRenderer传入的颜色
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _CoreWidth;
            float _Hardness;
            float _FlowSpeed;
            float _FlowIntensity;
            float _FlowFrequency;
            float _EmissionGain;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color; // 传递顶点颜色
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // --- 1. 计算光束形状 (两侧渐隐，中间实) ---
                // LineRenderer的UV.y 从0到1，0.5是中心
                float d = abs(i.uv.y - 0.5) * 2.0; // 将范围转换到 0(中心) -> 1(边缘)
                float shape = 1.0 - d; // 反转：1(中心) -> 0(边缘)
                
                // 使用指数函数控制中心有多"细"
                shape = pow(shape, _CoreWidth); 
                
                // 使用平滑阶梯函数让边缘更柔和或更硬
                shape = smoothstep(0.0, 1.0, shape * _Hardness);

                // --- 2. 计算能量流动效果 (沿X轴) ---
                // 使用正弦波模拟能量脉冲
                float flow = sin(i.uv.x * _FlowFrequency - _Time.y * _FlowSpeed);
                // 将正弦波(-1~1) 映射到 (0~1) 并应用强度
                flow = (flow * 0.5 + 0.5) * _FlowIntensity;
                
                // --- 3. 颜色合成 ---
                // 基础颜色 = 顶点颜色 * 形状系数
                float4 finalColor = i.color;
                
                // 将流动的高亮叠加到Alpha或者亮度上
                // 这里我们让流动部分比基础颜色更亮（模拟高能脉冲）
                float brightness = 1.0 + flow; 
                
                finalColor.rgb *= brightness * _EmissionGain;
                finalColor.a *= shape; // Alpha由形状控制

                // 防止完全透明部分的像素被写入（优化）
                clip(finalColor.a - 0.01);

                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                return finalColor;
            }
            ENDCG
        }
    }
}
