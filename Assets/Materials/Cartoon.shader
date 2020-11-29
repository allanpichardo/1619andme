Shader "Unlit/Cartoon"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Ambient ("Ambient", float) = 0.25
        _Diffuse ("Diffuse", float) = 1.0
        _Specular ("Specular", float) = 1
        _SpecularPower ("Specular Power", float) = 32.0
        _Banding ("Banding", float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal: NORMAL;
                float3 viewDirection: TEXCOORD1;
                float3 lightDirection: TEXCOORD2;
                float2 depth: TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _Ambient;
            float _Diffuse;
            float _Specular;
            float _SpecularPower;
            float _Banding;
            sampler2D _PaletteTex;
            float _PaletteCompensation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                COMPUTE_EYEDEPTH(v.vertex);
                o.viewDirection = normalize(WorldSpaceViewDir(v.vertex));
                o.lightDirection = normalize(WorldSpaceLightDir(v.vertex));
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            float3 Posterize(float steps, float value) {
                return floor(value * steps) / steps;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 lightDirection = i.lightDirection;
                float3 viewDirection = i.viewDirection;
                
                fixed4 col = tex2D(_MainTex, i.uv);
                UNITY_APPLY_FOG(i.fogCoord, col);
     
                float3 ambient = saturate(col * _LightColor0) * _Ambient;
                
                float falloff = (dot(lightDirection, i.normal));
                falloff = Posterize(_Banding, falloff);
                float3 diffuse = max(-0.01, falloff * col * _LightColor0 * _Diffuse);
                
                float specular = max(dot(reflect(-viewDirection, i.normal), lightDirection), 0);
                specular = Posterize(_Banding, specular);
                specular = pow(specular,_SpecularPower) * _LightColor0 * _Specular;
                
                float4 final = float4(ambient+diffuse+specular,1);
                return final;
            }
            ENDCG
        }
    }
}
