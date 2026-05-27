Shader "Hidden/BasicBlit"
{
    SubShader
    {
        Cull Front ZWrite Off ZTest Always

        Pass
        {
			Blend [_SrcBlend] [_DstBlend]
			ColorMask [_ColorMask]

            Stencil 
            {
                Ref [_Ref]
                Comp [_Comparison]
                Pass [_Operation]
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile __ SOBEL
            
            #include "UnityCG.cginc"
            #include "MiskCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
				DefineTransform
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;

                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            half _EffectSize;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            half4 _MainTex_ST;
            half4 _MainTex_TexelSize;

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_InitialTex);
            half4 _InitialTex_ST;
            half4 _InitialTex_TexelSize;

			DefineCoords

			inline half GetColor(half2 coord, half2 shift)
			{
				return FetchTexelAtWithShift(coord, shift).a * 100.0f;
			}

			inline half Sobel(half2 coord, half w, half h, out half4 main)
			{
				half n4 = GetColor(coord, 0.0f);
				half n0 = GetColor(coord, half2(-w, -h));
				half n1 = GetColor(coord, half2(0.0, -h));
				half n2 = GetColor(coord, half2(w, -h));
				half n3 = GetColor(coord, half2(-w, 0.0));
				half n5 = GetColor(coord, half2(w, 0.0));
				half n6 = GetColor(coord, half2(-w, h));
				half n7 = GetColor(coord, half2(0.0, h));
				half n8 = GetColor(coord, half2(w, h));

				half sobel_edge_h = n2 + (2.0f * n5) + n8 - (n0 + 2.0 * n3 + n6);
				half sobel_edge_v = n0 + (2.0f * n1) + n2 - (n6 + 2.0 * n7 + n8);
				half sobel = (sobel_edge_h * sobel_edge_h) + (sobel_edge_v * sobel_edge_v);

				main = n4;

				return saturate(sobel);
			}

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
                o.vertex = UnityObjectToClipPos(v.vertex);

				PostprocessCoords

                ComputeScreenShift
					
				CheckY

                o.uv = ComputeScreenPos(o.vertex);
				o.uv.xy *= _Scale;
				
#if UNITY_UV_STARTS_AT_TOP
				ModifyUV
#endif
                return o;
            }
            
            half4 frag (v2f i) : SV_Target
            {

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 uv = i.uv.xy / i.uv.w;

#if SOBEL
				half4 main;
				half sobel = Sobel(uv, _MainTex_TexelSize.x, _MainTex_TexelSize.y, main);

				half mask = main.a > 0.01f && sobel < 0.01f;

				clip(mask - 0.1f);
				return 0;
#else
				half4 texel = FetchTexel(uv);

				return texel;
#endif
            }
            ENDCG
        }
    }
}
