// Made with Amplify Shader Editor v1.9.9.12
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vefects/Lite_SH_Vefects_Unlit_Flipbook_BIRP_New"
{
	Properties
	{
		[Space(13)][Header(Main Texture)][Space(13)] _MainTexture( "Main Texture", 2D ) = "white" {}
		_UVS( "UV S", Vector ) = ( 1, 1, 0, 0 )
		_UVP( "UV P", Vector ) = ( 0, 0, 0, 0 )
		[HDR] _R( "R", Color ) = ( 1, 0.9719134, 0.5896226, 0 )
		[HDR] _G( "G", Color ) = ( 1, 0.7230805, 0.25, 0 )
		[HDR] _B( "B", Color ) = ( 0.5943396, 0.259371, 0.09812209, 0 )
		[HDR] _Outline( "Outline", Color ) = ( 0.2169811, 0.03320287, 0.02354041, 0 )
		[Header(TextureProps)][Space(13)] _Intensity( "Intensity", Range( 0, 5 ) ) = 1
		_ErosionSmoothness( "Erosion Smoothness", Range( 0.1, 15 ) ) = 0.1
		_FlatColor( "Flat Color", Range( 0, 1 ) ) = 0
		_UVDS1( "UV D S", Vector ) = ( 1, 1, 0, 0 )
		[Space(13)][Header(Distortion)][Space(13)] _DistortionTexture( "Distortion Texture", 2D ) = "white" {}
		_UVDP1( "UV D P", Vector ) = ( 0.1, -0.2, 0, 0 )
		_DistortionLerp( "Distortion Lerp", Range( 0, 0.1 ) ) = 0
		[Header(SecondDistortion)][Space(13)] _DistortionSecond( "DistortionSecond", 2D ) = "white" {}
		_SecondDistortionLerp( "SecondDistortionLerp", Range( 0.5, 1 ) ) = 0.5
		_UVDS( "UV D S", Vector ) = ( 1, 1, 0, 0 )
		_UVDP( "UV D P", Vector ) = ( 0.1, -0.2, 0, 0 )
		[Space(13)][Header(AR)][Space(13)] _Cull( "Cull", Float ) = 2
		_Src( "Src", Float ) = 5
		_Dst( "Dst", Float ) = 10
		_ZWrite( "ZWrite", Float ) = 0
		_ZTest( "ZTest", Float ) = 2

	}

	SubShader
	{
		

		

		Tags { "RenderType"="Transparent" "Queue"="Transparent" }

	LOD 0

		ZWrite [_ZWrite]
		Cull [_Cull]
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add

		

		Blend [_Src] [_Dst], One Zero
		BlendOp Add, Add
		

		CGINCLUDE
			#pragma target 3.5
			// ensure rendering platforms toggle list is visible

			float4 ComputeClipSpacePosition( float2 screenPosNorm, float deviceDepth )
			{
				float4 positionCS = float4( screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0 );
			#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
			#endif
				return positionCS;
			}
		ENDCG

		
		Pass
		{
			
			Name "Unlit"
			Tags { "LightMode"="ForwardBase" }

			Cull [_Cull]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Offset 0 , 0
			ColorMask RGBA
			Blend [_Src] [_Dst], One OneMinusSrcAlpha
			BlendOp Add, Add

			

			CGPROGRAM
				#define ASE_SURFACE_TRANSPARENT
				#define ASE_VERSION 19912

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"

				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_COLOR


				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS float4 pos : SV_POSITION;
					float4 ase_color : COLOR;
					float4 ase_texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				uniform float _Src;
				uniform float _Dst;
				uniform float _ZTest;
				uniform float _ZWrite;
				uniform float _Cull;
				uniform float4 _Outline;
				uniform float4 _B;
				uniform sampler2D _MainTexture;
				uniform float2 _UVP;
				uniform float2 _UVS;
				uniform sampler2D _DistortionTexture;
				uniform float2 _UVDP;
				uniform float2 _UVDS;
				uniform float _DistortionLerp;
				uniform float4 _G;
				uniform float4 _R;
				uniform float _FlatColor;
				uniform float _Intensity;
				uniform sampler2D _DistortionSecond;
				uniform float2 _UVDP1;
				uniform float2 _UVDS1;
				uniform float _SecondDistortionLerp;
				uniform float _ErosionSmoothness;


				
				v2f vert( appdata v  )
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.ase_color = v.ase_color;
					o.ase_texcoord = v.ase_texcoord;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = v.normal;
					v.tangent = v.tangent;

					o.pos = UnityObjectToClipPos( v.vertex );

					#if defined( ASE_SHADOWS )
						UNITY_TRANSFER_SHADOW( o, v.texcoord );
					#endif
					return o;
				}

				half4 frag( v2f IN 
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
				) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID( IN );
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

					float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					float4 ScreenPos = ComputeScreenPos( ClipPos );

					float2 texCoord19 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner27 = ( 1.0 * _Time.y * _UVP + ( texCoord19 * _UVS ));
					float2 texCoord11 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner15 = ( 1.0 * _Time.y * _UVDP + ( texCoord11 * _UVDS ));
					float2 lerpResult26 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, panner15 )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
					float2 DistortionRegister34 = ( panner27 + lerpResult26 );
					float4 tex2DNode45 = tex2D( _MainTexture, DistortionRegister34 );
					float4 lerpResult97 = lerp( _Outline , _B , tex2DNode45.b);
					float4 lerpResult112 = lerp( lerpResult97 , _G , tex2DNode45.g);
					float4 lerpResult111 = lerp( lerpResult112 , _R , tex2DNode45.r);
					float4 lerpResult88 = lerp( ( IN.ase_color * lerpResult111 ) , IN.ase_color , _FlatColor);
					float2 texCoord95 = IN.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner71 = ( 1.0 * _Time.y * _UVDP1 + ( texCoord95 * _UVDS1 ));
					float4 SecondDistortion108 = ( tex2D( _DistortionSecond, panner71 ) + _SecondDistortionLerp );
					
					float mainTex_alpha48 = tex2DNode45.a;
					float smoothstepResult55 = smoothstep( IN.ase_texcoord.z , ( IN.ase_texcoord.z + _ErosionSmoothness ) , mainTex_alpha48);
					float mainTex_VC_alha52 = IN.ase_color.a;
					float OpacityRegister61 = ( smoothstepResult55 * mainTex_VC_alha52 );
					

					float3 Color = ( ( lerpResult88 * _Intensity ) * SecondDistortion108 ).rgb;
					float Alpha = OpacityRegister61;
					half AlphaClipThreshold = 0.5;
					half AlphaClipThresholdShadow = 0.5;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = IN.pos.z;
					#endif

					#ifdef _ALPHATEST_ON
						clip( Alpha - AlphaClipThreshold );
					#endif

				#if defined( ASE_SURFACE_TRANSPARENT ) || defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, 1.0 );
				#endif
				}
			ENDCG
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual
			AlphaToMask Off

			CGPROGRAM
				#define ASE_SURFACE_TRANSPARENT
				#define ASE_VERSION 19912

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_shadowcaster
				#ifndef UNITY_PASS_SHADOWCASTER
					#define UNITY_PASS_SHADOWCASTER
				#endif
				#include "UnityCG.cginc"

				#include "UnityShaderVariables.cginc"
				#define ASE_NEEDS_TEXTURE_COORDINATES0
				#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0


				#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
					#define ASE_SV_DEPTH SV_DepthLessEqual
					#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
				#else
					#define ASE_SV_DEPTH SV_Depth
					#define ASE_SV_POSITION_QUALIFIERS
				#endif

				struct appdata
				{
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float4 ase_texcoord : TEXCOORD0;
					float4 ase_color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					ASE_SV_POSITION_QUALIFIERS UNITY_POSITION( pos );
					V2F_SHADOW_CASTER_NOPOS
					float4 ase_texcoord1 : TEXCOORD1;
					float4 ase_color : COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};

				#ifdef UNITY_STANDARD_USE_DITHER_MASK
					sampler3D _DitherMaskLOD;
				#endif

				uniform float _Src;
				uniform float _Dst;
				uniform float _ZTest;
				uniform float _ZWrite;
				uniform float _Cull;
				uniform float _ErosionSmoothness;
				uniform sampler2D _MainTexture;
				uniform float2 _UVP;
				uniform float2 _UVS;
				uniform sampler2D _DistortionTexture;
				uniform float2 _UVDP;
				uniform float2 _UVDS;
				uniform float _DistortionLerp;


				
				v2f vert( appdata v  )
				{
					UNITY_SETUP_INSTANCE_ID( v );
					v2f o;
					UNITY_INITIALIZE_OUTPUT( v2f, o );
					UNITY_TRANSFER_INSTANCE_ID( v, o );
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

					o.ase_texcoord1 = v.ase_texcoord;
					o.ase_color = v.ase_color;

					#ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
					#else
						float3 defaultVertexValue = float3(0, 0, 0);
					#endif
					float3 vertexValue = defaultVertexValue;
					#ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
					#else
						v.vertex.xyz += vertexValue;
					#endif
					v.vertex.w = 1;
					v.normal = v.normal;
					v.tangent = v.tangent;

					TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
					return o;
				}

				half4 frag( v2f IN 
							#if defined( ASE_WRITE_DEPTH )
								, out float outputDepth : SV_Depth
							#endif
							) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(IN);

					#ifdef LOD_FADE_CROSSFADE
						UNITY_APPLY_DITHER_CROSSFADE(IN.pos.xy);
					#endif

					float2 texCoord19 = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner27 = ( 1.0 * _Time.y * _UVP + ( texCoord19 * _UVS ));
					float2 texCoord11 = IN.ase_texcoord1.xy * float2( 1,1 ) + float2( 0,0 );
					float2 panner15 = ( 1.0 * _Time.y * _UVDP + ( texCoord11 * _UVDS ));
					float2 lerpResult26 = lerp( float2( 0,0 ) , ( ( (tex2D( _DistortionTexture, panner15 )).rg + -0.5 ) * 2.0 ) , _DistortionLerp);
					float2 DistortionRegister34 = ( panner27 + lerpResult26 );
					float4 tex2DNode45 = tex2D( _MainTexture, DistortionRegister34 );
					float mainTex_alpha48 = tex2DNode45.a;
					float smoothstepResult55 = smoothstep( IN.ase_texcoord1.z , ( IN.ase_texcoord1.z + _ErosionSmoothness ) , mainTex_alpha48);
					float mainTex_VC_alha52 = IN.ase_color.a;
					float OpacityRegister61 = ( smoothstepResult55 * mainTex_VC_alha52 );
					

					float Alpha = OpacityRegister61;
					half AlphaClipThreshold = 0.5;
					half AlphaClipThresholdShadow = 0.5;

					#if defined( ASE_WRITE_DEPTH )
						outputDepth = IN.pos.z;
					#endif

					#ifdef _ALPHATEST_SHADOW_ON
						if (unity_LightShadowBias.z != 0.0)
							clip(Alpha - AlphaClipThresholdShadow);
						#ifdef _ALPHATEST_ON
						else
							clip(Alpha - AlphaClipThreshold);
						#endif
					#else
						#ifdef _ALPHATEST_ON
							clip(Alpha - AlphaClipThreshold);
						#endif
					#endif

					#ifdef UNITY_STANDARD_USE_DITHER_MASK
						half alphaRef = tex3D(_DitherMaskLOD, float3(IN.pos.xy*0.25,Alpha*0.9375)).a;
						clip(alphaRef - 0.01);
					#endif

					SHADOW_CASTER_FRAGMENT(IN)
				}
			ENDCG
		}
		
	}
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
	
	Fallback Off
}
/*ASEBEGIN
Version=19912
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":10,"pos":[-4480,-1728],"params":["Inherit","False","1992","995","Distortion","18","34","31","27","26","25","24","23","22","20","19","18","17","16","15","14","13","12","11","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":11,"pos":[-4416,-1040],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":12,"pos":[-4160,-912],"params":["Inherit","False","Property","_UVDS","UV D S","16","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":13,"pos":[-4160,-1040],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":14,"pos":[-3904,-912],"params":["Inherit","False","Property","_UVDP","UV D P","17","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1,-0.2","0.1,-0.2","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":15,"pos":[-3904,-1040],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":16,"pos":[-3648,-1040],"params":["Inherit","True","Property","_DistortionTexture","Distortion Texture","11","0","Create","True","0","0","0","False","3","Space(13)","Header(Distortion)","Space(13)","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":17,"pos":[-3264,-1040],"params":["Inherit","False","True","True","False","False","1","0","COLOR","0,0,0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":18,"pos":[-3520,-1552],"params":["Inherit","False","Property","_UVS","UV S","1","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":19,"pos":[-3776,-1680],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":20,"pos":[-3264,-1552],"params":["Inherit","False","Property","_UVP","UV P","2","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":22,"pos":[-3264,-1168],"params":["Inherit","False","Property","_DistortionLerp","Distortion Lerp","13","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0.1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":23,"pos":[-3264,-1296],"params":["Inherit","False","Constant","_Vector0","Vector 0","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":24,"pos":[-3008,-1040],"params":["Inherit","False","ConstantBiasScale","-1","","1","63208df05c83e8e49a48ffbdce2e43a0","0","3","3","FLOAT2","0,0","False","1","FLOAT","-0.5","False","2","FLOAT","2","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":25,"pos":[-3520,-1680],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":26,"pos":[-2880,-1296],"params":["Inherit","False","3","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":27,"pos":[-3264,-1680],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":31,"pos":[-2880,-1680],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":36,"pos":[-2416,-1728],"params":["Inherit","False","1896","1857.979","Color","16","48","45","84","112","111","106","105","101","97","96","93","92","88","52","47","38","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":34,"pos":[-2720,-1680],"params":["Inherit","False","DistortionRegister","-1","True","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":38,"pos":[-2304,-640],"params":["Inherit","False","34","DistortionRegister","1","0","OBJECT","","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":21,"pos":[-4464,-320],"params":["Inherit","False","1538.791","442.8129","Opacity","8","61","58","57","55","53","51","46","28","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":45,"pos":[-1792,-640],"params":["Inherit","True","Property","_MainTexture","Main Texture","0","0","Create","True","0","0","0","False","3","Space(13)","Header(Main Texture)","Space(13)","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":46,"pos":[-4160,-208],"params":["Inherit","False","Property","_ErosionSmoothness","Erosion Smoothness","8","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1","1.57","0.1","15","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":47,"pos":[-1280,-1024],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":48,"pos":[-1408,-640],"params":["Inherit","False","mainTex_alpha","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":51,"pos":[-3856,-128],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":52,"pos":[-1280,-768],"params":["Inherit","False","mainTex_VC_alha","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":53,"pos":[-3968,-272],"params":["Inherit","False","48","mainTex_alpha","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":28,"pos":[-4432,-208],"params":["Inherit","False","0","4","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":57,"pos":[-3600,-280],"params":["Inherit","False","52","mainTex_VC_alha","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":55,"pos":[-3728,-192],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":58,"pos":[-3408,-192],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":63,"pos":[336,-48],"params":["Inherit","False","1243","166","AR","5","110","80","78","82","83","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":65,"pos":[-4464,-704],"params":["Inherit","False","1665.348","371.0714","SecondDistortion","9","108","104","103","102","98","95","94","85","71","","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":61,"pos":[-3120,-128],"params":["Inherit","False","OpacityRegister","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PannerNode, AmplifyShaderEditor","id":71,"pos":[-3904,-624],"params":["Inherit","False","3","0","FLOAT2","0,0","False","2","FLOAT2","0,0","False","1","FLOAT","1","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":85,"pos":[-3904,-496],"params":["Inherit","False","Property","_UVDP1","UV D P","12","0","Create","True","0","0","0","False","0","False","Object","-1","","0.1,-0.2","0.1,-0.2","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":88,"pos":[-768,-640],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":90,"pos":[-384,0],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":92,"pos":[-2304,-1152],"params":["Inherit","False","Property","_B","B","5","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","0.5943396,0.259371,0.09812209,0","0.2641509,0.2616589,0.2554289,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":93,"pos":[-2304,-896],"params":["Inherit","False","Property","_Outline","Outline","6","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","0.2169811,0.03320287,0.02354041,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":94,"pos":[-4160,-496],"params":["Inherit","False","Property","_UVDS1","UV D S","10","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1","1,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor","id":95,"pos":[-4416,-624],"params":["Inherit","False","0","-1","2","3","2","SAMPLER2D","","False","0","FLOAT2","1,1","False","1","FLOAT2","0,0","False","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":96,"pos":[-2304,-1408],"params":["Inherit","False","Property","_G","G","4","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","1,0.7230805,0.25,0","1,0.3523919,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":97,"pos":[-1792,-1280],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":98,"pos":[-4160,-624],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":99,"pos":[-384,-256],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":101,"pos":[-2304,-1664],"params":["Inherit","False","Property","_R","R","3","1","[HDR]","Create","True","0","0","0","False","0","False","Object","-1","","1,0.9719134,0.5896226,0","0.3679245,0.3679245,0.3679245,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":102,"pos":[-3552,-656],"params":["Inherit","True","Property","_DistortionSecond","DistortionSecond","14","1","[Header]","Create","True","1","SecondDistortion","0","0","False","1","Space(13)","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":103,"pos":[-3536,-448],"params":["Inherit","False","Property","_SecondDistortionLerp","SecondDistortionLerp","15","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","0","0.5","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":104,"pos":[-3232,-512],"params":["Inherit","False","2","2","0","COLOR","0,0,0,0","False","1","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":105,"pos":[-768,-1152],"params":["Inherit","True","2","2","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":106,"pos":[-1152,-384],"params":["Inherit","False","Property","_FlatColor","Flat Color","9","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":107,"pos":[-768,-128],"params":["Inherit","False","Property","_Intensity","Intensity","7","1","[Header]","Create","True","1","TextureProps","0","0","False","1","Space(13)","False","Object","-1","","1","1","0","5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":111,"pos":[-1152,-1664],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":112,"pos":[-1408,-1408],"params":["Inherit","True","3","0","COLOR","0,0,0,0","False","1","COLOR","0,0,0,0","False","2","FLOAT","0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":78,"pos":[640,0],"params":["Inherit","False","Property","_Src","Src","19","0","Create","True","0","0","0","True","0","False","Object","-1","","5","5","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":110,"pos":[896,0],"params":["Inherit","False","Property","_Dst","Dst","20","0","Create","True","0","0","0","True","0","False","Object","-1","","10","10","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":82,"pos":[1408,0],"params":["Inherit","False","Property","_ZTest","ZTest","22","0","Create","True","0","0","0","True","0","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":83,"pos":[1152,0],"params":["Inherit","False","Property","_ZWrite","ZWrite","21","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":84,"pos":[-768,0],"params":["Inherit","False","108","SecondDistortion","1","0","OBJECT","","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":73,"pos":[-768,128],"params":["Inherit","False","61","OpacityRegister","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":80,"pos":[384,0],"params":["Inherit","False","Property","_Cull","Cull","18","0","Create","True","0","0","0","True","3","Space(13)","Header(AR)","Space(13)","False","Object","-1","","2","2","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":108,"pos":[-3024,-512],"params":["Inherit","False","SecondDistortion","-1","True","1","0","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":116,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ExtraPrePass","0","0","ExtraPrePass","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","12","all","0","False","True","1","1","False","","0","False","","0","1","False","","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":117,"pos":[0,0],"params":["Float","False","True","-1","3","AmplifyShaderEditor.MaterialInspector","0","7","Vefects/Lite_SH_Vefects_Unlit_Flipbook_BIRP_New","0770190933193b94aaa3065e307002fa","True","Unlit","0","1","Unlit","8","True","True","1","1","True","_Src","0","True","_Dst","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","True","True","0","True","_Cull","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","1","True","_ZWrite","False","False","False","True","2","RenderType=Transparent=RenderType","Queue=Transparent=Queue=0","True","3","True","12","all","0","True","True","1","5","True","_Src","10","True","_Dst","1","1","False","","10","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","True","True","0","True","_Cull","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","True","True","2","True","_ZWrite","True","3","True","_ZTest","True","True","0","False","","0","False","","False","True","1","LightMode=ForwardBase","False","False","0","","0","0","Standard","10","Surface","1","639223348078465202","  Keep Alpha","0","0","  Blend","0","0","Alpha Clipping","0","0","  Use Shadow Threshold","0","0","Cast Shadows","1","0","Write Depth","0","0","  Conservative","0","0","Extra Pre Pass","0","0","Vertex Position","1","0","0","3","False","True","True","False","","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":118,"pos":[0,0],"params":["Float","False","False","-1","3","AmplifyShaderEditor.MaterialInspector","0","1","New Amplify Shader","0770190933193b94aaa3065e307002fa","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","1","RenderType=Opaque=RenderType","True","3","True","12","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":114,"pos":[-2329.735,-2158.397],"params":["Inherit","False","100","100","Lite version","0","Lite version","0,0,0,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":113,"pos":[336,-224],"params":["Inherit","False","304","100","Lush was here! <3","0","Lush was here! <3","0,0,0,1","0","0"]}
{"wire":[13,0,11,0]}
{"wire":[13,1,12,0]}
{"wire":[15,0,13,0]}
{"wire":[15,2,14,0]}
{"wire":[16,1,15,0]}
{"wire":[17,0,16,0]}
{"wire":[24,3,17,0]}
{"wire":[25,0,19,0]}
{"wire":[25,1,18,0]}
{"wire":[26,0,23,0]}
{"wire":[26,1,24,0]}
{"wire":[26,2,22,0]}
{"wire":[27,0,25,0]}
{"wire":[27,2,20,0]}
{"wire":[31,0,27,0]}
{"wire":[31,1,26,0]}
{"wire":[34,0,31,0]}
{"wire":[45,1,38,0]}
{"wire":[48,0,45,4]}
{"wire":[51,0,28,3]}
{"wire":[51,1,46,0]}
{"wire":[52,0,47,4]}
{"wire":[55,0,53,0]}
{"wire":[55,1,28,3]}
{"wire":[55,2,51,0]}
{"wire":[58,0,55,0]}
{"wire":[58,1,57,0]}
{"wire":[61,0,58,0]}
{"wire":[71,0,98,0]}
{"wire":[71,2,85,0]}
{"wire":[88,0,105,0]}
{"wire":[88,1,47,0]}
{"wire":[88,2,106,0]}
{"wire":[90,0,99,0]}
{"wire":[90,1,84,0]}
{"wire":[97,0,93,0]}
{"wire":[97,1,92,0]}
{"wire":[97,2,45,3]}
{"wire":[98,0,95,0]}
{"wire":[98,1,94,0]}
{"wire":[99,0,88,0]}
{"wire":[99,1,107,0]}
{"wire":[102,1,71,0]}
{"wire":[104,0,102,0]}
{"wire":[104,1,103,0]}
{"wire":[105,0,47,0]}
{"wire":[105,1,111,0]}
{"wire":[111,0,112,0]}
{"wire":[111,1,101,0]}
{"wire":[111,2,45,1]}
{"wire":[112,0,97,0]}
{"wire":[112,1,96,0]}
{"wire":[112,2,45,2]}
{"wire":[108,0,104,0]}
{"wire":[117,0,90,0]}
{"wire":[117,7,73,0]}
ASEEND*/
//CHKSM=E68171B721BD84D08AA7FED7B2202A53A593FB5F