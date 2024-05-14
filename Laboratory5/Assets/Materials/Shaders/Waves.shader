Shader "Universal Render Pipeline/ImprovedWaves" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _WaveA ("Wave A (dir, steepness, wavelength)", Vector) = (1,0,0.5,10)
        _WaveB ("Wave B", Vector) = (0,1,0.25,20)
        _WaveC ("Wave C", Vector) = (1,1,0.15,10)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        Pass {
            Stencil {
                Ref 1
                Comp always
                Pass replace
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _WaveA, _WaveB, _WaveC;
            float _Glossiness;
            float _Metallic;
            float4 _Color;

            float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal) {
                float steepness = wave.z;
                float wavelength = wave.w;
                float k = 2 * UNITY_PI / wavelength;
                float c = sqrt(9.8 / k);
                float2 d = normalize(wave.xy);
                float f = k * (dot(d, p.xz) - c * _Time.y);
                float a = steepness / k;
                tangent += float3(
                    -d.x * d.x * (steepness * sin(f)),
                    d.x * (steepness * cos(f)),
                    -d.x * d.y * (steepness * sin(f))
                );
                binormal += float3(
                    -d.x * d.y * (steepness * sin(f)),
                    d.y * (steepness * cos(f)),
                    -d.y * d.y * (steepness * sin(f))
                );
                return float3(
                    d.x * (a * cos(f)),
                    a * sin(f),
                    d.y * (a * cos(f))
                );
            }

            v2f vert(appdata v) {
                v2f o;
                float3 gridPoint = v.vertex.xyz;
                float3 tangent = float3(1, 0, 0);
                float3 binormal = float3(0, 0, 1);
                float3 p = gridPoint;
                p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
                p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);
                float3 normal = normalize(cross(binormal, tangent));
                o.vertex = UnityObjectToClipPos(float4(p, 1));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(normal);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                float3 viewDir = normalize(UnityWorldSpaceViewDir(i.vertex.xyz));
                float3 h = normalize(viewDir + float3(0, 0, 1));
                float nDotH = saturate(dot(i.worldNormal, h));
                float nDotL = dot(float3(0, 1, 0), i.worldNormal);
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                float3 spec = _Glossiness * pow(nDotH, 50) * c.rgb;
                fixed4 col = c + float4(spec, 0);
                return col;
            }
            ENDCG
        }
    }
}