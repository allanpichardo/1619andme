Shader "Unlit/ColorPalette"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorPalette ("Color Palette Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _ColorPalette;
            
            float ToGrayscale(float4 color) {
                return 0.21 * color.x + 0.71 * color.y + 0.07 * color.z;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float t = ToGrayscale(col);
                return tex2D(_ColorPalette, float2(t,0));
            }
            ENDCG
        }
    }
}
