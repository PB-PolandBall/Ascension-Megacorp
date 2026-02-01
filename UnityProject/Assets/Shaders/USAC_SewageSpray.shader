Shader "USAC/SewageSpray"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.35, 0.45, 0.25, 0.9)
        _Speed ("Flow Speed", Float) = 2.0
        _NoiseScale ("Noise Scale", Float) = 3.0
        _Distortion ("Distortion Strength", Range(0, 1)) = 0.2
        _Alpha ("Alpha Multiplier", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Speed;
            float _NoiseScale;
            float _Distortion;
            float _Alpha;

            // 简单的伪随机噪声
            float hash(float2 p) {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 简单的 Value Noise
            float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float res = lerp(lerp(hash(i), hash(i + float2(1.0, 0.0)), f.x),
                               lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x), f.y);
                return res;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // 锥形化处理：底部宽，顶部更宽
                // uv.y=0 是喷口（底部），uv.y=1 是末端（顶部）
                // 底部 0.4 (5倍于之前的0.08)，顶部扩展到 1.2
                float taper = lerp(0.4, 1.2, pow(v.uv.y, 0.6));
                
                float4 pos = v.vertex;
                // 只缩放 X 轴宽度，保持 Z 轴（喷射方向）不变
                // 关键：将顶点居中，避免偏移
                float centeredX = (v.uv.x - 0.5) * 2.0; // -1 到 1
                pos.x = centeredX * taper * 0.5; // 乘回 0.5 恢复原始尺度

                o.vertex = UnityObjectToClipPos(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 滚动 UV 以模拟水流
                float2 flowUV = i.uv;
                flowUV.y -= _Time.y * _Speed * 2.0; // 快速流动

                // 多层噪声模拟水的湍流
                float n1 = noise(flowUV * _NoiseScale);
                float n2 = noise(flowUV * _NoiseScale * 2.0 + float2(10.0, 5.0));
                float n3 = noise(flowUV * _NoiseScale * 0.5 - float2(3.0, 7.0));
                float liquid = (n1 * 0.5 + n2 * 0.3 + n3 * 0.2);

                // 水柱的核心：中心密实，边缘淡出
                // 使用更锐利的中心衰减
                float distFromCenter = abs(i.uv.x - 0.5) * 2.0; // 0 at center, 1 at edge
                float coreDensity = 1.0 - pow(distFromCenter, 1.5);
                
                // 随高度变化：底部实心，顶部散开成水雾
                // uv.y=0 (喷口) -> 高密度
                // uv.y=1 (末端) -> 低密度，雾化
                float heightFactor = 1.0 - pow(i.uv.y, 1.2);
                
                // 边缘的水花效果：顶部边缘噪声更强
                float edgeSpray = liquid * (1.0 - heightFactor) * (1.0 - coreDensity * 0.5);

                // 组合 Alpha
                float baseAlpha = coreDensity * heightFactor;
                float sprayAlpha = edgeSpray * 0.8;
                float finalAlpha = saturate(baseAlpha + sprayAlpha);
                
                // 底部和顶部淡出
                float tipFade = smoothstep(0.0, 0.05, i.uv.y) * smoothstep(1.0, 0.85, i.uv.y);
                
                // 边缘硬边 -> 软边过渡
                float edgeFade = smoothstep(0.5, 0.3, distFromCenter);
                
                fixed4 col = _Color * i.color;
                
                // 根据高度调整颜色：底部颜色更深更实，顶部偏白（水雾）
                col.rgb = lerp(col.rgb, col.rgb * 1.3 + 0.15, i.uv.y * 0.5);
                
                // 噪声细节
                col.rgb += liquid * 0.15;
                
                col.a *= finalAlpha * tipFade * edgeFade * _Alpha;

                return col;
            }
            ENDCG
        }
    }
}
