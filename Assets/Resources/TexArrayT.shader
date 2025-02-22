Shader "Custom/TexArrayT"
{
    Properties
    {
        _MainTex("Texture Array", 2DArray) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float index : TEXCOORD1;
            };

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv.xy;
                o.index = v.uv.z; 
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(i.uv, i.index));
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
