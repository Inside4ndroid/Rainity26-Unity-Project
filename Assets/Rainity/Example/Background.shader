Shader "Custom/Background" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_Color2("Color 2", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags {
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
			"Queue" = "Geometry"
		}
		LOD 200

		// ------------------------------------------------------------------
		// Forward Lit Pass
		Pass {
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _Color;
				half4 _Color2;
				half _Glossiness;
				half _Metallic;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				float2 uv         : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionHCS : SV_POSITION;
				float2 uv          : TEXCOORD0;
				float3 positionWS  : TEXCOORD1;
				float3 normalWS    : TEXCOORD2;
				float  fogFactor   : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes IN) {
				Varyings OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

				VertexPositionInputs posInputs    = GetVertexPositionInputs(IN.positionOS.xyz);
				VertexNormalInputs   normalInputs = GetVertexNormalInputs(IN.normalOS);

				OUT.positionHCS = posInputs.positionCS;
				OUT.positionWS  = posInputs.positionWS;
				OUT.normalWS    = normalInputs.normalWS;
				OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
				OUT.fogFactor   = ComputeFogFactor(posInputs.positionCS.z);
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target {
				UNITY_SETUP_INSTANCE_ID(IN);

				half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
				half4 tint     = lerp(_Color, _Color2, IN.uv.y);
				half4 albedo   = texColor * tint;

				InputData inputData = (InputData)0;
				inputData.positionWS              = IN.positionWS;
				inputData.normalWS                = normalize(IN.normalWS);
				inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(IN.positionWS);
				inputData.shadowCoord             = TransformWorldToShadowCoord(IN.positionWS);
				inputData.fogCoord                = IN.fogFactor;
				inputData.vertexLighting          = half3(0, 0, 0);
				inputData.bakedGI                 = SampleSH(inputData.normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionHCS);
				inputData.shadowMask              = half4(1, 1, 1, 1);

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo     = albedo.rgb;
				surfaceData.alpha      = albedo.a;
				surfaceData.metallic   = _Metallic;
				surfaceData.smoothness = _Glossiness;
				surfaceData.normalTS   = half3(0, 0, 1);
				surfaceData.occlusion  = 1.0;
				surfaceData.emission   = half3(0, 0, 0);
				surfaceData.specular   = half3(0, 0, 0);

				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				color.rgb   = MixFog(color.rgb, IN.fogFactor);
				return color;
			}
			ENDHLSL
		}

		// ------------------------------------------------------------------
		// Shadow Caster Pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

			// Global per-draw properties set by URP for shadow rendering.
			// Declared explicitly here as URP 17 (Unity 6) does not expose them
			// through the standard includes in custom passes.
			float3 _LightDirection;
			float3 _LightPosition;

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _Color;
				half4 _Color2;
				half _Glossiness;
				half _Metallic;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS   : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes IN) {
				Varyings OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

				float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
				float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDir = normalize(_LightPosition - positionWS);
				#else
					float3 lightDir = _LightDirection;
				#endif

				float4 posCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
				#if UNITY_REVERSED_Z
					posCS.z = min(posCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					posCS.z = max(posCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif
				OUT.positionCS = posCS;
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target { return 0; }
			ENDHLSL
		}

		// ------------------------------------------------------------------
		// Depth Only Pass
		Pass {
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }

			ZWrite On
			ColorMask R
			Cull Back

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _MainTex_ST;
				half4 _Color;
				half4 _Color2;
				half _Glossiness;
				half _Metallic;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			Varyings vert(Attributes IN) {
				Varyings OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
				OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
				return OUT;
			}

			half4 frag(Varyings IN) : SV_Target { return 0; }
			ENDHLSL
		}
	}
	FallBack "Universal Render Pipeline/Lit"
}
