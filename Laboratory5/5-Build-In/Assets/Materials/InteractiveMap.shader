Shader "Custom/InteractiveMap"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _Amount;
        sampler2D _HeightMap;

        float2 _VertexMin;
        float2 _VertexMax;
        float2 _UVMin;
        float2 _UVMax;

        // Cropping
        float2 _CropSize;
        float2 _CropOffset;
        
        float2 vertexToUV(float4 vertex)
        {
            return (vertex.xz - _VertexMin) / (_VertexMax - _VertexMin) * (_UVMax - _UVMin) + _UVMin;
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float4 getVertex(float4 vertex)
        {
            float3 normal = float3(0, 1, 0);
            float2 texcoord = vertexToUV(vertex);
            fixed height = tex2Dlod(_HeightMap, float4(texcoord, 0, 0)).r;
            vertex.xyz += normal * height * _Amount;
            return vertex;
        }

        void vert(inout appdata_base v)
        {
            // v.vertex.xz: [_VertexMin, _VertexMax]
            // cropped.xz : [croppedMin, croppedMax]
            float2 croppedMin = _CropOffset;
            float2 croppedMax = croppedMin + _CropSize;
            float4 cropped = v.vertex;
            cropped.xz = (v.vertex.xz - _VertexMin) / (_VertexMax - _VertexMin)
                * (croppedMax - croppedMin) + croppedMin;
            float3 bitangent = float3(1, 0, 0);
            float3 normal    = float3(0, 1, 0);
            float3 tangent   = float3(0, 0, 1);
            float offset = 0.01;
                    
            float4 vertexBitangent = getVertex(cropped + float4(bitangent * offset, 0) );
            float4 vertex          = getVertex(cropped);
            float4 vertexTangent   = getVertex(cropped + float4(tangent   * offset, 0) );
            float3 newBitangent = (vertexBitangent - vertex).xyz;
            float3 newTangent   = (vertexTangent   - vertex).xyz;
            v.normal = cross(newTangent, newBitangent);
            v.vertex.y = vertex.y;
            v.texcoord = float4(vertexToUV(cropped), 0,0);
        }
                
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
