Shader "Custom/WallhackSilhouette"
{
    Properties
    {
        _Color ("Silhouette Color", Color) = (1, 0.2, 0.2, 0.6)
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 2.0
        _PulseMin ("Pulse Min Alpha", Range(0, 1)) = 0.3
        _PulseMax ("Pulse Max Alpha", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Overlay+100" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        
        // Duvar arkasinda gorunen silhouette pass
        Pass
        {
            Name "WALLHACK"
            
            ZWrite Off
            ZTest Always          // Derinlik testini atla — her seyin onunde ciz
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing   // VR single-pass instanced icin
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO     // VR stereo desteği
            };

            fixed4 _Color;
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                // Mesh'i hafifce buyut — outline hissi versin
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, float3(0,0,1)));
                
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Nabiz efekti — gorunurluk zamanla atar
                float pulse = lerp(_PulseMin, _PulseMax, 
                    (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5));
                
                fixed4 col = _Color;
                col.a = pulse;
                return col;
            }
            ENDCG
        }
    }
    
    FallBack Off
}
