// USAC 覆盖层着色器
// 使用预乘 Alpha + 低阈值裁剪，兼顾性能和边缘质量
Shader "Custom/USAC_Overlay" {
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.01
    }
    
    SubShader {
        // 渲染队列略高于普通透明物体
        Tags { 
            "Queue" = "Transparent+10" 
            "RenderType" = "TransparentCutout" 
            "IgnoreProjector" = "true"
        }
        
        Pass {
            Name "Overlay"
            
            // 保持深度写入以获得正确遮挡
            ZWrite On
            
            // 预乘 Alpha 混合：One OneMinusSrcAlpha
            // 比标准混合更适合处理边缘
            Blend One OneMinusSrcAlpha
            
            // 关闭背面剔除
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Cutoff;
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target {
                float4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // 极低阈值裁剪，只丢弃完全透明的像素
                clip(col.a - _Cutoff);
                
                // 预乘 RGB，消除边缘白边
                col.rgb *= col.a;
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "VertexLit"
}
