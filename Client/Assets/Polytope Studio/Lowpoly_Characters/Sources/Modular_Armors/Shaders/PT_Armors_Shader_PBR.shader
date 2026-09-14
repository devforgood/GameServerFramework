// Made with Amplify Shader Editor v1.9.9.9
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Polytope Studio/ PT_Medieval Armors Shader PBR"
{
	Properties
	{
		[HDR] _SKINCOLOR( "SKIN COLOR", Color ) = ( 2.02193, 1.0081, 0.6199315, 0 )
		_SKINSMOOTHNESS( "SKIN SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _EYESCOLOR( "EYES COLOR", Color ) = ( 0.0734529, 0.1320755, 0.05046281, 1 )
		_EYESSMOOTHNESS( "EYES SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _HAIRCOLOR( "HAIR COLOR", Color ) = ( 0.5943396, 0.3518379, 0.1093361, 0 )
		_HAIRSMOOTHNESS( "HAIR SMOOTHNESS", Range( 0, 1 ) ) = 0.1
		[HDR] _SCLERACOLOR( "SCLERA COLOR", Color ) = ( 0.9056604, 0.8159487, 0.8159487, 0 )
		_SCLERASMOOTHNESS( "SCLERA SMOOTHNESS", Range( 0, 1 ) ) = 0.5
		[HDR] _LIPSCOLOR( "LIPS COLOR", Color ) = ( 0.8301887, 0.3185886, 0.2780349, 0 )
		_LIPSSMOOTHNESS( "LIPS SMOOTHNESS", Range( 0, 1 ) ) = 0.4
		[HDR] _SCARSCOLOR( "SCARS COLOR", Color ) = ( 0.8490566, 0.5037117, 0.3884835, 0 )
		_SCARSSMOOTHNESS( "SCARS SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _METAL1COLOR( "METAL 1 COLOR", Color ) = ( 2, 0.682353, 0.1960784, 0 )
		_METAL1METALLIC( "METAL 1 METALLIC", Range( 0, 1 ) ) = 0.65
		_METAL1SMOOTHNESS( "METAL 1 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _METAL2COLOR( "METAL 2 COLOR", Color ) = ( 0.4674706, 0.4677705, 0.5188679, 0 )
		_METAL2METALLIC( "METAL 2 METALLIC", Range( 0, 1 ) ) = 0.65
		_METAL2SMOOTHNESS( "METAL 2 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _METAL3COLOR( "METAL 3 COLOR", Color ) = ( 0.4383232, 0.4383232, 0.4716981, 0 )
		_METAL3METALLIC( "METAL 3 METALLIC", Range( 0, 1 ) ) = 0.65
		_METAL3SMOOTHNESS( "METAL 3 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _LEATHER1COLOR( "LEATHER 1 COLOR", Color ) = ( 0.4811321, 0.2041155, 0.08851016, 1 )
		_LEATHER1SMOOTHNESS( "LEATHER 1 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _LEATHER2COLOR( "LEATHER 2 COLOR", Color ) = ( 0.4245283, 0.190437, 0.09011215, 1 )
		_LEATHER2SMOOTHNESS( "LEATHER 2 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _LEATHER3COLOR( "LEATHER 3 COLOR", Color ) = ( 0.1698113, 0.04637412, 0.02963688, 1 )
		_LEATHER3SMOOTHNESS( "LEATHER 3 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HDR] _CLOTH1COLOR( "CLOTH 1 COLOR", Color ) = ( 0.1465379, 0.282117, 0.3490566, 0 )
		[HDR] _CLOTH2COLOR( "CLOTH 2 COLOR", Color ) = ( 1, 0, 0, 0 )
		[HDR] _CLOTH3COLOR( "CLOTH 3 COLOR", Color ) = ( 0.8773585, 0.6337318, 0.3434941, 0 )
		[HDR] _GEMS1COLOR( "GEMS 1 COLOR", Color ) = ( 0.3773585, 0, 0.06650025, 0 )
		_GEMS1SMOOTHNESS( "GEMS 1 SMOOTHNESS", Range( 0, 1 ) ) = 1
		[HDR] _GEMS2COLOR( "GEMS 2 COLOR", Color ) = ( 0.2023368, 0, 0.4339623, 0 )
		_GEMS2SMOOTHNESS( "GEMS 2 SMOOTHNESS", Range( 0, 1 ) ) = 0
		[HDR] _GEMS3COLOR( "GEMS 3 COLOR", Color ) = ( 0, 0.1132075, 0.01206957, 0 )
		_GEMS3SMOOTHNESS( "GEMS 3 SMOOTHNESS", Range( 0, 1 ) ) = 0
		[HDR] _FEATHERS1COLOR( "FEATHERS 1 COLOR", Color ) = ( 0.7735849, 0.492613, 0.492613, 0 )
		[HDR] _FEATHERS2COLOR( "FEATHERS 2 COLOR", Color ) = ( 0.6792453, 0, 0, 0 )
		[HDR] _FEATHERS3COLOR( "FEATHERS 3 COLOR", Color ) = ( 0, 0.1793142, 0.7264151, 0 )
		[HDR] _CUSTOM1COLOR( "CUSTOM 1 COLOR", Color ) = ( 0.0734529, 0.1320755, 0.05046281, 1 )
		_CUSTOM1SMOOTHNESS( "CUSTOM 1 SMOOTHNESS", Range( 0, 1 ) ) = 0.7
		[HDR] _CUSTOM2COLOR( "CUSTOM 2 COLOR", Color ) = ( 0.5943396, 0.3518379, 0.1093361, 0 )
		_CUSTOM2SMOOTHNESS( "CUSTOM 2 SMOOTHNESS", Range( 0, 1 ) ) = 0.1
		[HDR] _CUSTOM3COLOR( "CUSTOM 3 COLOR", Color ) = ( 2.02193, 1.0081, 0.6199315, 0 )
		_CUSTOM3SMOOTHNESS( "CUSTOM 3 SMOOTHNESS", Range( 0, 1 ) ) = 0.3
		[HideInInspector] _Texture0( "Texture 0", 2D ) = "white" {}
		[HideInInspector] _Texture8( "Texture 0", 2D ) = "white" {}
		[HideInInspector] _Texture1( "Texture 1", 2D ) = "white" {}
		[HideInInspector] _Texture6( "Texture 6", 2D ) = "white" {}
		[HideInInspector] _Texture3( "Texture 3", 2D ) = "white" {}
		[HideInInspector] _Texture5( "Texture 5", 2D ) = "white" {}
		[HideInInspector][HDR] _Texture2( "Texture 2", 2D ) = "white" {}
		[HideInInspector] _Texture4( "Texture 4", 2D ) = "white" {}
		[HideInInspector] _Texture7( "Texture 7", 2D ) = "white" {}
		[HDR] _COATOFARMSCOLOR( "COAT OF ARMS COLOR", Color ) = ( 1, 0, 0, 0 )
		[NoScaleOffset] _COATOFARMSMASK( "COAT OF ARMS MASK", 2D ) = "black" {}
		_OCCLUSION( "OCCLUSION", Range( 0, 1 ) ) = 0.5
		[Toggle] _MetalicOn( "Metalic On", Float ) = 1
		[Toggle] _SmoothnessOn( "Smoothness On", Float ) = 1
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.5
		#define ASE_VERSION 19909
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
			float2 uv2_texcoord2;
		};

		uniform sampler2D _Texture2;
		uniform float4 _Texture2_ST;
		uniform float4 _GEMS3COLOR;
		uniform sampler2D _Texture7;
		uniform float4 _Texture7_ST;
		uniform float4 _GEMS2COLOR;
		uniform float4 _GEMS1COLOR;
		uniform float4 _FEATHERS3COLOR;
		uniform sampler2D _Texture4;
		uniform float4 _Texture4_ST;
		uniform float4 _FEATHERS2COLOR;
		uniform float4 _FEATHERS1COLOR;
		uniform float4 _CLOTH3COLOR;
		uniform sampler2D _Texture5;
		uniform float4 _Texture5_ST;
		uniform float4 _CLOTH2COLOR;
		uniform float4 _CLOTH1COLOR;
		uniform float4 _LEATHER3COLOR;
		uniform sampler2D _Texture3;
		uniform float4 _Texture3_ST;
		uniform float4 _LEATHER2COLOR;
		uniform float4 _LEATHER1COLOR;
		uniform float4 _METAL3COLOR;
		uniform sampler2D _Texture6;
		uniform float4 _Texture6_ST;
		uniform float4 _METAL2COLOR;
		uniform float4 _METAL1COLOR;
		uniform float4 _SCARSCOLOR;
		uniform sampler2D _Texture1;
		uniform float4 _Texture1_ST;
		uniform float4 _LIPSCOLOR;
		uniform float4 _SCLERACOLOR;
		uniform float4 _EYESCOLOR;
		uniform sampler2D _Texture0;
		uniform float4 _Texture0_ST;
		uniform float4 _HAIRCOLOR;
		uniform float4 _SKINCOLOR;
		uniform float4 _CUSTOM1COLOR;
		uniform sampler2D _Texture8;
		uniform float4 _Texture8_ST;
		uniform float4 _CUSTOM2COLOR;
		uniform float4 _CUSTOM3COLOR;
		uniform float4 _COATOFARMSCOLOR;
		uniform sampler2D _COATOFARMSMASK;
		uniform float _MetalicOn;
		uniform float _METAL3METALLIC;
		uniform float _METAL2METALLIC;
		uniform float _METAL1METALLIC;
		uniform float _SmoothnessOn;
		uniform float _GEMS3SMOOTHNESS;
		uniform float _GEMS2SMOOTHNESS;
		uniform float _GEMS1SMOOTHNESS;
		uniform float _LEATHER3SMOOTHNESS;
		uniform float _LEATHER2SMOOTHNESS;
		uniform float _LEATHER1SMOOTHNESS;
		uniform float _METAL3SMOOTHNESS;
		uniform float _METAL2SMOOTHNESS;
		uniform float _METAL1SMOOTHNESS;
		uniform float _SCARSSMOOTHNESS;
		uniform float _LIPSSMOOTHNESS;
		uniform float _SCLERASMOOTHNESS;
		uniform float _EYESSMOOTHNESS;
		uniform float _HAIRSMOOTHNESS;
		uniform float _SKINSMOOTHNESS;
		uniform float _CUSTOM1SMOOTHNESS;
		uniform float _CUSTOM2SMOOTHNESS;
		uniform float _CUSTOM3SMOOTHNESS;
		uniform float _OCCLUSION;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Texture2 = i.uv_texcoord * _Texture2_ST.xy + _Texture2_ST.zw;
			float4 tex2DNode199 = tex2D( _Texture2, uv_Texture2 );
			float2 uv_Texture7 = i.uv_texcoord * _Texture7_ST.xy + _Texture7_ST.zw;
			float4 tex2DNode222 = tex2D( _Texture7, uv_Texture7 );
			float3 temp_cast_0 = (tex2DNode222.b).xxx;
			float temp_output_215_0 = saturate( ( 1.0 - ( ( distance( temp_cast_0 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult219 = lerp( float4( 0,0,0,0 ) , ( tex2DNode199 * _GEMS3COLOR ) , temp_output_215_0);
			float3 temp_cast_1 = (tex2DNode222.g).xxx;
			float temp_output_216_0 = saturate( ( 1.0 - ( ( distance( temp_cast_1 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult214 = lerp( lerpResult219 , ( tex2DNode199 * _GEMS2COLOR ) , temp_output_216_0);
			float3 temp_cast_2 = (tex2DNode222.r).xxx;
			float temp_output_213_0 = saturate( ( 1.0 - ( ( distance( temp_cast_2 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult217 = lerp( lerpResult214 , ( tex2DNode199 * _GEMS1COLOR ) , temp_output_213_0);
			float2 uv_Texture4 = i.uv_texcoord * _Texture4_ST.xy + _Texture4_ST.zw;
			float4 tex2DNode181 = tex2D( _Texture4, uv_Texture4 );
			float3 temp_cast_3 = (tex2DNode181.b).xxx;
			float4 lerpResult182 = lerp( lerpResult217 , ( tex2DNode199 * _FEATHERS3COLOR ) , saturate( ( 1.0 - ( ( distance( temp_cast_3 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) ));
			float3 temp_cast_4 = (tex2DNode181.g).xxx;
			float4 lerpResult189 = lerp( lerpResult182 , ( tex2DNode199 * _FEATHERS2COLOR ) , saturate( ( 1.0 - ( ( distance( temp_cast_4 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) ));
			float3 temp_cast_5 = (tex2DNode181.r).xxx;
			float4 lerpResult184 = lerp( lerpResult189 , ( tex2DNode199 * _FEATHERS1COLOR ) , saturate( ( 1.0 - ( ( distance( temp_cast_5 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) ));
			float2 uv_Texture5 = i.uv_texcoord * _Texture5_ST.xy + _Texture5_ST.zw;
			float4 tex2DNode170 = tex2D( _Texture5, uv_Texture5 );
			float3 temp_cast_6 = (tex2DNode170.b).xxx;
			float4 lerpResult171 = lerp( lerpResult184 , ( tex2DNode199 * _CLOTH3COLOR ) , saturate( ( 1.0 - ( ( distance( temp_cast_6 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) ));
			float3 temp_cast_7 = (tex2DNode170.g).xxx;
			float4 lerpResult178 = lerp( lerpResult171 , ( tex2DNode199 * _CLOTH2COLOR ) , saturate( ( 1.0 - ( ( distance( temp_cast_7 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) ));
			float3 temp_cast_8 = (tex2DNode170.r).xxx;
			float4 lerpResult173 = lerp( lerpResult178 , ( tex2DNode199 * _CLOTH1COLOR ) , saturate( ( 1.0 - ( ( distance( temp_cast_8 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) ));
			float2 uv_Texture3 = i.uv_texcoord * _Texture3_ST.xy + _Texture3_ST.zw;
			float4 tex2DNode159 = tex2D( _Texture3, uv_Texture3 );
			float3 temp_cast_9 = (tex2DNode159.b).xxx;
			float temp_output_165_0 = saturate( ( 1.0 - ( ( distance( temp_cast_9 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult160 = lerp( lerpResult173 , ( tex2DNode199 * _LEATHER3COLOR ) , temp_output_165_0);
			float3 temp_cast_10 = (tex2DNode159.g).xxx;
			float temp_output_158_0 = saturate( ( 1.0 - ( ( distance( temp_cast_10 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult167 = lerp( lerpResult160 , ( tex2DNode199 * _LEATHER2COLOR ) , temp_output_158_0);
			float3 temp_cast_11 = (tex2DNode159.r).xxx;
			float temp_output_157_0 = saturate( ( 1.0 - ( ( distance( temp_cast_11 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult162 = lerp( lerpResult167 , ( tex2DNode199 * _LEATHER1COLOR ) , temp_output_157_0);
			float2 uv_Texture6 = i.uv_texcoord * _Texture6_ST.xy + _Texture6_ST.zw;
			float4 tex2DNode121 = tex2D( _Texture6, uv_Texture6 );
			float3 temp_cast_12 = (tex2DNode121.b).xxx;
			float temp_output_117_0 = saturate( ( 1.0 - ( ( distance( temp_cast_12 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult118 = lerp( lerpResult162 , ( tex2DNode199 * _METAL3COLOR ) , temp_output_117_0);
			float3 temp_cast_13 = (tex2DNode121.g).xxx;
			float temp_output_127_0 = saturate( ( 1.0 - ( ( distance( temp_cast_13 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult128 = lerp( lerpResult118 , ( tex2DNode199 * _METAL2COLOR ) , temp_output_127_0);
			float3 temp_cast_14 = (tex2DNode121.r).xxx;
			float temp_output_123_0 = saturate( ( 1.0 - ( ( distance( temp_cast_14 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult122 = lerp( lerpResult128 , ( tex2DNode199 * _METAL1COLOR ) , temp_output_123_0);
			float2 uv_Texture1 = i.uv_texcoord * _Texture1_ST.xy + _Texture1_ST.zw;
			float4 tex2DNode144 = tex2D( _Texture1, uv_Texture1 );
			float3 temp_cast_15 = (tex2DNode144.b).xxx;
			float temp_output_145_0 = saturate( ( 1.0 - ( ( distance( temp_cast_15 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult148 = lerp( lerpResult122 , ( tex2DNode199 * _SCARSCOLOR ) , temp_output_145_0);
			float3 temp_cast_16 = (tex2DNode144.g).xxx;
			float temp_output_149_0 = saturate( ( 1.0 - ( ( distance( temp_cast_16 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult151 = lerp( lerpResult148 , ( tex2DNode199 * _LIPSCOLOR ) , temp_output_149_0);
			float3 temp_cast_17 = (tex2DNode144.r).xxx;
			float temp_output_150_0 = saturate( ( 1.0 - ( ( distance( temp_cast_17 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult153 = lerp( lerpResult151 , ( tex2DNode199 * _SCLERACOLOR ) , temp_output_150_0);
			float2 uv_Texture0 = i.uv_texcoord * _Texture0_ST.xy + _Texture0_ST.zw;
			float4 tex2DNode37 = tex2D( _Texture0, uv_Texture0 );
			float3 temp_cast_18 = (tex2DNode37.b).xxx;
			float temp_output_71_0 = saturate( ( 1.0 - ( ( distance( temp_cast_18 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult73 = lerp( lerpResult153 , ( tex2DNode199 * _EYESCOLOR ) , temp_output_71_0);
			float3 temp_cast_19 = (tex2DNode37.g).xxx;
			float temp_output_67_0 = saturate( ( 1.0 - ( ( distance( temp_cast_19 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult69 = lerp( lerpResult73 , ( tex2DNode199 * _HAIRCOLOR ) , temp_output_67_0);
			float3 temp_cast_20 = (tex2DNode37.r).xxx;
			float temp_output_63_0 = saturate( ( 1.0 - ( ( distance( temp_cast_20 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult62 = lerp( lerpResult69 , ( tex2DNode199 * _SKINCOLOR ) , temp_output_63_0);
			float2 uv_Texture8 = i.uv_texcoord * _Texture8_ST.xy + _Texture8_ST.zw;
			float4 tex2DNode597 = tex2D( _Texture8, uv_Texture8 );
			float3 temp_cast_21 = (tex2DNode597.b).xxx;
			float temp_output_599_0 = saturate( ( 1.0 - ( ( distance( temp_cast_21 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult613 = lerp( lerpResult62 , ( tex2DNode199 * _CUSTOM1COLOR ) , temp_output_599_0);
			float3 temp_cast_22 = (tex2DNode597.g).xxx;
			float temp_output_601_0 = saturate( ( 1.0 - ( ( distance( temp_cast_22 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult614 = lerp( lerpResult613 , ( tex2DNode199 * _CUSTOM2COLOR ) , temp_output_601_0);
			float3 temp_cast_23 = (tex2DNode597.r).xxx;
			float temp_output_608_0 = saturate( ( 1.0 - ( ( distance( temp_cast_23 , float3( 0,0,0 ) ) - 0.1 ) / max( 0.0, 1E-05 ) ) ) );
			float4 lerpResult616 = lerp( lerpResult614 , ( tex2DNode199 * _CUSTOM3COLOR ) , temp_output_608_0);
			float2 uv1_COATOFARMSMASK10 = i.uv2_texcoord2;
			float temp_output_9_0 = ( 1.0 - tex2D( _COATOFARMSMASK, uv1_COATOFARMSMASK10 ).a );
			float4 temp_cast_24 = (temp_output_9_0).xxxx;
			float4 temp_output_1_0_g92 = temp_cast_24;
			float4 color25 = IsGammaSpace() ? float4( 0, 0, 0, 0 ) : float4( 0, 0, 0, 0 );
			float4 temp_output_2_0_g92 = color25;
			float temp_output_11_0_g92 = distance( temp_output_1_0_g92 , temp_output_2_0_g92 );
			float2 _Vector0 = float2(1.6,1);
			float4 lerpResult21_g92 = lerp( _COATOFARMSCOLOR , temp_output_1_0_g92 , saturate( ( ( temp_output_11_0_g92 - _Vector0.x ) / max( _Vector0.y, 1E-05 ) ) ));
			float4 lerpResult64 = lerp( lerpResult616 , lerpResult21_g92 , ( 1.0 - temp_output_9_0 ));
			o.Albedo = lerpResult64.rgb;
			float lerpResult315 = lerp( 0.0 , _METAL3METALLIC , temp_output_117_0);
			float lerpResult319 = lerp( lerpResult315 , _METAL2METALLIC , temp_output_127_0);
			float lerpResult316 = lerp( lerpResult319 , _METAL1METALLIC , temp_output_123_0);
			o.Metallic = (( _MetalicOn )?( lerpResult316 ):( 0.0 ));
			float lerpResult342 = lerp( 0.0 , _GEMS3SMOOTHNESS , temp_output_215_0);
			float lerpResult338 = lerp( lerpResult342 , _GEMS2SMOOTHNESS , temp_output_216_0);
			float lerpResult340 = lerp( lerpResult338 , _GEMS1SMOOTHNESS , temp_output_213_0);
			float lerpResult336 = lerp( lerpResult340 , _LEATHER3SMOOTHNESS , temp_output_165_0);
			float lerpResult332 = lerp( lerpResult336 , _LEATHER2SMOOTHNESS , temp_output_158_0);
			float lerpResult334 = lerp( lerpResult332 , _LEATHER1SMOOTHNESS , temp_output_157_0);
			float lerpResult327 = lerp( lerpResult334 , _METAL3SMOOTHNESS , temp_output_117_0);
			float lerpResult331 = lerp( lerpResult327 , _METAL2SMOOTHNESS , temp_output_127_0);
			float lerpResult328 = lerp( lerpResult331 , _METAL1SMOOTHNESS , temp_output_123_0);
			float lerpResult321 = lerp( lerpResult328 , _SCARSSMOOTHNESS , temp_output_145_0);
			float lerpResult325 = lerp( lerpResult321 , _LIPSSMOOTHNESS , temp_output_149_0);
			float lerpResult322 = lerp( lerpResult325 , _SCLERASMOOTHNESS , temp_output_150_0);
			float lerpResult306 = lerp( lerpResult322 , _EYESSMOOTHNESS , temp_output_71_0);
			float lerpResult304 = lerp( lerpResult306 , _HAIRSMOOTHNESS , temp_output_67_0);
			float lerpResult302 = lerp( lerpResult304 , _SKINSMOOTHNESS , temp_output_63_0);
			float lerpResult604 = lerp( lerpResult302 , _CUSTOM1SMOOTHNESS , temp_output_599_0);
			float lerpResult605 = lerp( lerpResult604 , _CUSTOM2SMOOTHNESS , temp_output_601_0);
			float lerpResult611 = lerp( lerpResult605 , _CUSTOM3SMOOTHNESS , temp_output_608_0);
			o.Smoothness = (( _SmoothnessOn )?( lerpResult611 ):( 0.0 ));
			o.Occlusion =  (1.0 + ( _OCCLUSION - 0.0 ) * ( 0.5 - 1.0 ) / ( 1.0 - 0.0 ) );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback Off
}
/*ASEBEGIN
Version=19909
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;-18395.06,107.5165;Inherit;False;2305.307;694.8573;Comment;14;223;219;218;222;221;215;216;214;213;217;220;229;230;231;GEMS COLORS;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;200;-1776,-1856;Inherit;False;680.9902;282.8338;Comment;2;198;199;BASE TEXTURE;1,1,1,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;198;-1776,-1808;Inherit;True;Property;_Texture2;Texture 2;51;2;[HideInInspector];[HDR];Create;True;0;0;0;False;0;False;004b7cabc9421734bb88a754e99fd641;004b7cabc9421734bb88a754e99fd641;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;220;-18345.06,572.19;Inherit;True;Property;_Texture7;Texture 7;53;1;[HideInInspector];Create;True;0;0;0;False;0;False;7d388dc79e41c6f498271666b30c0630;7d388dc79e41c6f498271666b30c0630;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;222;-18051.41,572.374;Inherit;True;Property;_TextureSample6;Texture Sample 6;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;199;-1440,-1808;Inherit;True;Property;_TextureSample2;Texture Sample 2;14;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;231;-17873.24,175.481;Inherit;False;Property;_GEMS3COLOR;GEMS 3 COLOR;34;1;[HDR];Create;True;0;0;0;False;0;False;0,0.1132075,0.01206957,0;0,0.1132075,0.01206957,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;215;-17673.51,621.2559;Inherit;False;Color Mask;-1;;45;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;-17646.8,157.5165;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;230;-17263.86,180.9097;Inherit;False;Property;_GEMS2COLOR;GEMS 2 COLOR;32;1;[HDR];Create;True;0;0;0;False;0;False;0.2023368,0,0.4339623,0;0.2023368,0,0.4339623,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;212;-15940.94,100.0198;Inherit;False;2305.3;694.8572;Comment;14;183;185;182;188;184;180;181;187;189;186;179;224;225;226;FEATHERS COLORS;0.735849,0.7152051,0.3158597,1;0;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;216;-17071.49,611.767;Inherit;False;Color Mask;-1;;46;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;219;-17470.98,377.9008;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;185;-15890.94,564.6931;Inherit;True;Property;_Texture4;Texture 4;52;1;[HideInInspector];Create;True;0;0;0;False;0;False;ae91646efc13ec44f8ccc5b5db2d6a8d;ae91646efc13ec44f8ccc5b5db2d6a8d;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;229;-16691.28,179.7326;Inherit;False;Property;_GEMS1COLOR;GEMS 1 COLOR;30;1;[HDR];Create;True;0;0;0;False;0;False;0.3773585,0,0.06650025,0;0.3773585,0,0.06650025,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;221;-17033.94,159.8954;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;181;-15597.29,564.877;Inherit;True;Property;_TextureSample5;Texture Sample 5;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;213;-16540,583.2526;Inherit;False;Color Mask;-1;;47;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;214;-16850.53,363.3687;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;218;-16457.13,164.9575;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;224;-15434.72,169.3589;Inherit;False;Property;_FEATHERS3COLOR;FEATHERS 3 COLOR;38;1;[HDR];Create;True;0;0;0;False;0;False;0,0.1793142,0.7264151,0;0,0.1793142,0.7264151,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;188;-15192.68,150.0198;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;225;-14843.42,170.8393;Inherit;False;Property;_FEATHERS2COLOR;FEATHERS 2 COLOR;37;1;[HDR];Create;True;0;0;0;False;0;False;0.6792453,0,0,0;0.6792453,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;208;-13458.26,101.9824;Inherit;False;2359.399;695.7338;Comment;14;168;175;173;178;170;172;169;174;176;177;171;209;211;210;CLOTH COLORS;0.4690726,0.7830189,0.47128,1;0;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;217;-16273.75,372.6839;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;187;-15204.95,608.944;Inherit;False;Color Mask;-1;;48;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;344;-17794.08,1639.719;Inherit;False;1768.5;211.4459;Comment;6;342;343;341;338;339;340;GEMS SMOOTHNESS;1,1,1,1;0;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;174;-13408.26,567.7161;Inherit;True;Property;_Texture5;Texture 5;50;1;[HideInInspector];Create;True;0;0;0;False;0;False;e4f1621d61032d045964d463b3806afe;e4f1621d61032d045964d463b3806afe;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;180;-14611.35,601.8626;Inherit;False;Color Mask;-1;;49;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;183;-14579.82,152.3987;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;343;-17744.08,1717.038;Inherit;False;Property;_GEMS3SMOOTHNESS;GEMS 3 SMOOTHNESS;35;0;Create;True;0;0;0;False;0;False;0;0.794;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;182;-15016.86,370.4042;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;226;-14298.42,166.8393;Inherit;False;Property;_FEATHERS1COLOR;FEATHERS 1 COLOR;36;1;[HDR];Create;True;0;0;0;False;0;False;0.7735849,0.492613,0.492613,0;0.7775454,0.9510008,0.9528302,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;179;-14090.69,579.3669;Inherit;False;Color Mask;-1;;50;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;211;-12946.7,169.5253;Inherit;False;Property;_CLOTH3COLOR;CLOTH 3 COLOR;29;1;[HDR];Create;True;0;0;0;False;0;False;0.8773585,0.6337318,0.3434941,0;0.5471698,0.5471122,0.5187789,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;342;-17444.96,1694.174;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;341;-17111.68,1717.219;Inherit;False;Property;_GEMS2SMOOTHNESS;GEMS 2 SMOOTHNESS;33;0;Create;True;0;0;0;False;0;False;0;0.762;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;186;-14003.01,157.4608;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;189;-14396.41,355.8722;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;170;-13060.51,566.8392;Inherit;True;Property;_TextureSample4;Texture Sample 4;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;204;-10950.96,81.85144;Inherit;False;2342.301;700.673;Comment;14;163;159;203;166;165;202;160;161;201;158;164;167;162;157;LEATHER COLORS;0.7735849,0.5371538,0.1788003,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;339;-16578.44,1718.975;Inherit;False;Property;_GEMS1SMOOTHNESS;GEMS 1 SMOOTHNESS;31;0;Create;True;0;0;0;False;0;False;1;0.755;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;184;-13819.64,365.1873;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;338;-16818.67,1695.166;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;345;-10281.75,1639.19;Inherit;False;1768.502;211.4459;Comment;6;337;336;335;332;333;334;LEATHER SMOOTHNESS;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;177;-12655.9,151.9825;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;210;-12318.56,167.2369;Inherit;False;Property;_CLOTH2COLOR;CLOTH 2 COLOR;28;1;[HDR];Create;True;0;0;0;False;0;False;1,0,0,0;0.6226415,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;176;-12683.81,613.3138;Inherit;False;Color Mask;-1;;63;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;163;-10900.96,552.5245;Inherit;True;Property;_Texture3;Texture 3;49;1;[HideInInspector];Create;True;0;0;0;False;0;False;9c0e067347abba2489817b3ce813c911;9c0e067347abba2489817b3ce813c911;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;169;-12080.59,606.2324;Inherit;False;Color Mask;-1;;71;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;171;-12480.08,372.3667;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;337;-10231.75,1716.508;Inherit;False;Property;_LEATHER3SMOOTHNESS;LEATHER 3 SMOOTHNESS;26;0;Create;True;0;0;0;False;0;False;0.3;0.35;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;209;-11733.9,176.3902;Inherit;False;Property;_CLOTH1COLOR;CLOTH 1 COLOR;27;1;[HDR];Create;True;0;0;0;False;0;False;0.1465379,0.282117,0.3490566,0;0.1465379,0.282117,0.3490566,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;340;-16258.63,1696.486;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;172;-12043.04,154.3614;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;-10570.31,546.7085;Inherit;True;Property;_TextureSample3;Texture Sample 3;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;178;-11859.63,357.8348;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;335;-9599.346,1716.689;Inherit;False;Property;_LEATHER2SMOOTHNESS;LEATHER 2 SMOOTHNESS;24;0;Create;True;0;0;0;False;0;False;0.3;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;-11553.91,581.3292;Inherit;False;Color Mask;-1;;80;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;336;-9932.623,1693.644;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;203;-10437.53,161.7001;Inherit;False;Property;_LEATHER3COLOR;LEATHER 3 COLOR;25;1;[HDR];Create;True;0;0;0;False;0;False;0.1698113,0.04637412,0.02963688,1;0.1226415,0.07025669,0.04570131,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;175;-11466.22,159.4234;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;165;-10191.21,595.5906;Inherit;False;Color Mask;-1;;72;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;194;-8397.117,81.08585;Inherit;False;2305.301;694.8571;Comment;14;130;121;126;117;118;127;120;128;119;122;123;195;196;197;METAL COLORS;0.259434,0.8569208,1,1;0;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;332;-9306.341,1694.636;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;333;-9017.06,1711.679;Inherit;False;Property;_LEATHER1SMOOTHNESS;LEATHER 1 SMOOTHNESS;22;0;Create;True;0;0;0;False;0;False;0.3;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;346;-7837.468,1617.395;Inherit;False;1768.499;211.4459;Comment;6;329;330;327;326;331;328;METAL SMOOTHNESS;1,1,1,1;0;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;202;-9846.154,165.072;Inherit;False;Property;_LEATHER2COLOR;LEATHER 2 COLOR;23;1;[HDR];Create;True;0;0;0;False;0;False;0.4245283,0.190437,0.09011215,1;0.2358491,0.05549391,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;173;-11282.86,367.1498;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;-10170.95,137.1077;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;130;-8347.117,545.759;Inherit;True;Property;_Texture6;Texture 6;48;1;[HideInInspector];Create;True;0;0;0;False;0;False;47efbf030a9bb7f428ba51b46a2fdd03;47efbf030a9bb7f428ba51b46a2fdd03;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;158;-9590.39,587.3054;Inherit;False;Color Mask;-1;;81;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;334;-8697.249,1689.19;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;-9989.884,352.2355;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;329;-7789.468,1695.713;Inherit;False;Property;_METAL3SMOOTHNESS;METAL 3 SMOOTHNESS;20;0;Create;True;0;0;0;False;0;False;0.7;0.677;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;-9552.838,134.2304;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;201;-9256.154,158.072;Inherit;False;Property;_LEATHER1COLOR;LEATHER 1 COLOR;21;1;[HDR];Create;True;0;0;0;False;0;False;0.4811321,0.2041155,0.08851016,1;0.3867925,0.2276603,0.129539,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;-8053.467,545.9429;Inherit;True;Property;_TextureSample8;Texture Sample 8;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;-9063.711,561.1985;Inherit;False;Color Mask;-1;;82;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;327;-7488.34,1671.849;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;-8976.024,139.2924;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;197;-7959.427,149.6947;Inherit;False;Property;_METAL3COLOR;METAL 3 COLOR;18;1;[HDR];Create;True;0;0;0;False;0;False;0.4383232,0.4383232,0.4716981,0;0.6320754,0.6320754,0.6320754,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;326;-7155.065,1694.894;Inherit;False;Property;_METAL2SMOOTHNESS;METAL 2 SMOOTHNESS;17;0;Create;True;0;0;0;False;0;False;0.7;0.651;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;-9369.433,349.9297;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;190;-5786.208,60.18156;Inherit;False;2305.296;707.5152;Comment;14;143;144;156;145;155;148;149;150;154;151;153;191;192;193;LIPS - SCARS - SCLERA COLORS;1,1,1,1;0;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;117;-7675.571,594.8251;Inherit;False;Color Mask;-1;;83;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;330;-6572.781,1689.884;Inherit;False;Property;_METAL1SMOOTHNESS;METAL 1 SMOOTHNESS;14;0;Create;True;0;0;0;False;0;False;0.7;0.683;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;196;-7295.61,136.1474;Inherit;False;Property;_METAL2COLOR;METAL 2 COLOR;15;1;[HDR];Create;True;0;0;0;False;0;False;0.4674706,0.4677705,0.5188679,0;0.1946867,0.2850214,0.3301887,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;331;-6862.062,1672.841;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;126;-7648.856,131.0858;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;347;-5204.773,1621.108;Inherit;False;1768.499;211.4459;Comment;6;324;320;325;322;321;323;LIPS - SCARS - SCLERA SMOOTHNESS;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;-8792.659,347.0186;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;127;-7100.835,576.2403;Inherit;False;Color Mask;-1;;84;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;-5712.379,559.0286;Inherit;True;Property;_Texture1;Texture 1;47;1;[HideInInspector];Create;True;0;0;0;False;0;False;9a688ecd3c6cbee4cb2a528bf72399d4;9a688ecd3c6cbee4cb2a528bf72399d4;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;195;-6732.036,154.556;Inherit;False;Property;_METAL1COLOR;METAL 1 COLOR;12;1;[HDR];Create;True;0;0;0;False;0;False;2,0.682353,0.1960784,0;0.8117647,0.3651657,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;120;-7035.995,133.4648;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;118;-7473.041,351.4701;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;328;-6252.969,1667.395;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;323;-5156.341,1698.426;Inherit;False;Property;_SCARSSMOOTHNESS;SCARS SMOOTHNESS;11;0;Create;True;0;0;0;False;0;False;0.3;0.608;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;-6546.868,560.433;Inherit;False;Color Mask;-1;;85;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;144;-5404.992,564.9833;Inherit;True;Property;_TextureSample1;Texture Sample 1;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;128;-6852.589,336.9381;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;320;-4522.371,1698.607;Inherit;False;Property;_LIPSSMOOTHNESS;LIPS SMOOTHNESS;9;0;Create;True;0;0;0;False;0;False;0.4;0.529;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;193;-5276.292,135.8387;Inherit;False;Property;_SCARSCOLOR;SCARS COLOR;10;1;[HDR];Create;True;0;0;0;False;0;False;0.8490566,0.5037117,0.3884835,0;0.8490566,0.5037117,0.3884835,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;119;-6459.181,138.5268;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;321;-4855.645,1675.562;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;-3284.644,71.51185;Inherit;False;2305.298;694.8571;Comment;13;77;37;74;71;75;67;41;73;69;62;581;582;583;SKIN - HAIR - EYES COLORS;1,0,0,1;0;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;145;-5012.501,609.8978;Inherit;True;Color Mask;-1;;86;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;156;-5031.696,161.4115;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;313;-2711.574,1613.886;Inherit;False;1768.499;211.4464;Comment;6;307;302;306;305;303;304;SKIN - EYES - HAIR SMOOTHNESS;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;324;-3940.084,1693.597;Inherit;False;Property;_SCLERASMOOTHNESS;SCLERA SMOOTHNESS;7;0;Create;True;0;0;0;False;0;False;0.5;0.816;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;192;-4663.813,130.0028;Inherit;False;Property;_LIPSCOLOR;LIPS COLOR;8;1;[HDR];Create;True;0;0;0;False;0;False;0.8301887,0.3185886,0.2780349,0;0.8301887,0.3185886,0.2780349,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;122;-6275.816,346.2531;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;325;-4229.366,1676.554;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-4442.702,584.3403;Inherit;True;Color Mask;-1;;87;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;-4388.793,157.3194;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;322;-3620.273,1671.108;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-4849.282,337.5115;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;191;-4083.874,145.6415;Inherit;False;Property;_SCLERACOLOR;SCLERA COLOR;6;1;[HDR];Create;True;0;0;0;False;0;False;0.9056604,0.8159487,0.8159487,0;0.8490566,0.6928622,0.6928622,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;-3234.644,536.1849;Inherit;True;Property;_Texture0;Texture 0;45;1;[HideInInspector];Create;True;0;0;0;False;0;False;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;307;-2661.574,1691.204;Inherit;False;Property;_EYESSMOOTHNESS;EYES SMOOTHNESS;3;0;Create;True;0;0;0;False;0;False;0.7;0.671;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;354;-7862.719,1064.29;Inherit;False;1768.498;211.4463;Comment;6;317;314;315;318;319;316;METAL METALLIC;1,1,1,1;0;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;150;-3939.59,552.1868;Inherit;True;Color Mask;-1;;88;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;151;-4243.684,339.6917;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;-3812.454,162.3207;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;37;-2944.998,538.3689;Inherit;True;Property;_TextureSample0;Texture Sample 0;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;10;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;305;-2029.171,1691.385;Inherit;False;Property;_HAIRSMOOTHNESS;HAIR SMOOTHNESS;5;0;Create;True;0;0;0;False;0;False;0.1;0.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;74;-2838.578,169.5337;Inherit;False;Property;_EYESCOLOR;EYES COLOR;2;1;[HDR];Create;True;0;0;0;False;0;False;0.0734529,0.1320755,0.05046281,1;0.0734529,0.1320755,0.05046281,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;306;-2362.446,1668.34;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;24;1136,-1424;Inherit;False;1262.249;589.4722;;7;16;18;26;25;17;9;10;COAT OF ARMS;1,0,0.7651567,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;317;-7812.719,1142.608;Inherit;False;Property;_METAL3METALLIC;METAL 3 METALLIC;19;0;Create;True;0;0;0;False;0;False;0.65;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;-3664.912,338.0065;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;71;-2618.101,582.2063;Inherit;True;Color Mask;-1;;89;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;-2198.202,182.6123;Inherit;False;Property;_HAIRCOLOR;HAIR COLOR;4;1;[HDR];Create;True;0;0;0;False;0;False;0.5943396,0.3518379,0.1093361,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;583;-2528.904,148.7538;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;595;-688,96;Inherit;False;2305.298;694.8571;Comment;14;616;615;614;613;612;610;609;608;606;603;601;599;597;596;SKIN - HAIR - EYES COLORS;1,0,0,1;0;0
Node;AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;598;-448,1632;Inherit;False;1768.499;211.4464;Comment;6;611;607;605;604;602;600;SKIN - EYES - HAIR SMOOTHNESS;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;303;-1440,1760;Inherit;False;Property;_SKINSMOOTHNESS;SKIN SMOOTHNESS;1;0;Create;True;0;0;0;False;0;False;0.3;0.357;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;304;-1712,1664;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;314;-7180.316,1141.789;Inherit;False;Property;_METAL2METALLIC;METAL 2 METALLIC;16;0;Create;True;0;0;0;False;0;False;0.65;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;315;-7513.59,1118.744;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;1184,-1344;Inherit;True;Property;_COATOFARMSMASK;COAT OF ARMS MASK;55;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;10;d294e9544b9eca64188ea9d2482ea8a1;d294e9544b9eca64188ea9d2482ea8a1;True;1;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;67;-2006.077,578.7616;Inherit;False;Color Mask;-1;;90;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;41;-1631.331,169.188;Inherit;False;Property;_SKINCOLOR;SKIN COLOR;0;1;[HDR];Create;True;0;0;0;False;0;False;2.02193,1.0081,0.6199315,0;0.8490566,0.6739655,0.4605732,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;302;-1127.075,1663.886;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;582;-1841.327,158.4496;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;73;-2347.718,336.1837;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;600;-400,1712;Inherit;False;Property;_CUSTOM1SMOOTHNESS;CUSTOM 1 SMOOTHNESS;40;0;Create;True;0;0;0;False;0;False;0.7;0.671;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;596;-656,560;Inherit;True;Property;_Texture8;Texture 0;46;1;[HideInInspector];Create;True;0;0;0;False;0;False;035d7aa8896fd004988e129310ea695a;b76fe68d69ca53f43a4e6f66d135dd90;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;318;-6599.226,1160.631;Inherit;False;Property;_METAL1METALLIC;METAL 1 METALLIC;13;0;Create;True;0;0;0;False;0;False;0.65;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;319;-6887.312,1119.736;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;1520,-1248;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;18;1904,-1024;Inherit;False;Constant;_Vector0;Vector 0;1;0;Create;True;0;0;0;False;0;False;1.6,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;26;1888,-1216;Inherit;False;Property;_COATOFARMSCOLOR;COAT OF ARMS COLOR;54;1;[HDR];Create;True;0;0;0;False;0;False;1,0,0,0;0.3631096,2.217881,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;25;1888,-1392;Inherit;False;Constant;_Color4;Color 4;1;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;69;-1747.119,334.3638;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;581;-1361.772,158.7538;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;63;-1392,560;Inherit;False;Color Mask;-1;;91;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;604;-48,1664;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;602;240,1712;Inherit;False;Property;_CUSTOM2SMOOTHNESS;CUSTOM 2 SMOOTHNESS;42;0;Create;True;0;0;0;False;0;False;0.1;0.52;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;603;-240,192;Inherit;False;Property;_CUSTOM1COLOR;CUSTOM 1 COLOR;39;1;[HDR];Create;True;0;0;0;False;0;False;0.0734529,0.1320755,0.05046281,1;0.0734529,0.1320755,0.05046281,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;597;-336,560;Inherit;True;Property;_TextureSample7;Texture Sample 0;4;1;[HideInInspector];Create;True;0;0;0;False;0;False;-1;b76fe68d69ca53f43a4e6f66d135dd90;b76fe68d69ca53f43a4e6f66d135dd90;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;316;-6278.221,1114.29;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;16;1712,-976;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;17;2144,-1248;Inherit;False;Replace Color;-1;;92;896dccb3016c847439def376a728b869;1,12,0;5;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;-1163.346,336.6786;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;599;-16,592;Inherit;True;Color Mask;-1;;93;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;605;528,1680;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;609;64,160;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;607;816,1696;Inherit;False;Property;_CUSTOM3SMOOTHNESS;CUSTOM 3 SMOOTHNESS;44;0;Create;True;0;0;0;False;0;False;0.3;0.357;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;606;400,192;Inherit;False;Property;_CUSTOM2COLOR;CUSTOM 2 COLOR;41;1;[HDR];Create;True;0;0;0;False;0;False;0.5943396,0.3518379,0.1093361,0;1,1,1,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;252;3520,1712;Inherit;False;Property;_OCCLUSION;OCCLUSION;56;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;584;3728,1040;Inherit;False;Property;_MetalicOn;Metalic On;57;0;Create;True;0;0;0;False;0;False;1;True;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;574;2624,-1232;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;576;2560,-928;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;601;592,592;Inherit;True;Color Mask;-1;;94;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;611;1136,1680;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;612;752,176;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;610;960,192;Inherit;False;Property;_CUSTOM3COLOR;CUSTOM 3 COLOR;43;1;[HDR];Create;True;0;0;0;False;0;False;2.02193,1.0081,0.6199315,0;0.8490566,0.6739655,0.4605732,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;613;224,384;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;579;3824,1712;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;593;3936,1056;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ToggleSwitchNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;585;3760,1552;Inherit;False;Property;_SmoothnessOn;Smoothness On;58;0;Create;True;0;0;0;False;0;False;1;True;Create;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;575;2592,-864;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;573;2656,-1184;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;608;1168,560;Inherit;True;Color Mask;-1;;95;eec747d987850564c95bde0e5a6d1867;0;4;1;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0.1;False;5;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;614;848,352;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;615;1232,176;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;577;2608,288;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;588;4096,1696;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;578;2656,256;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;591;3984,1504;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;592;3936,432;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;616;1424,352;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;594;3968,384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;589;4016,448;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;580;4096,480;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;64;2736,272;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;428;4144,288;Float;False;True;-1;3;;0;0;Standard;Polytope Studio/ PT_Medieval Armors Shader PBR;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;0;False;;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;ForwardOnly;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;13.63;1,0,0,0;VertexScale;False;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;222;0;220;0
WireConnection;199;0;198;0
WireConnection;215;3;222;3
WireConnection;223;0;199;0
WireConnection;223;1;231;0
WireConnection;216;3;222;2
WireConnection;219;1;223;0
WireConnection;219;2;215;0
WireConnection;221;0;199;0
WireConnection;221;1;230;0
WireConnection;181;0;185;0
WireConnection;213;3;222;1
WireConnection;214;0;219;0
WireConnection;214;1;221;0
WireConnection;214;2;216;0
WireConnection;218;0;199;0
WireConnection;218;1;229;0
WireConnection;188;0;199;0
WireConnection;188;1;224;0
WireConnection;217;0;214;0
WireConnection;217;1;218;0
WireConnection;217;2;213;0
WireConnection;187;3;181;3
WireConnection;180;3;181;2
WireConnection;183;0;199;0
WireConnection;183;1;225;0
WireConnection;182;0;217;0
WireConnection;182;1;188;0
WireConnection;182;2;187;0
WireConnection;179;3;181;1
WireConnection;342;1;343;0
WireConnection;342;2;215;0
WireConnection;186;0;199;0
WireConnection;186;1;226;0
WireConnection;189;0;182;0
WireConnection;189;1;183;0
WireConnection;189;2;180;0
WireConnection;170;0;174;0
WireConnection;184;0;189;0
WireConnection;184;1;186;0
WireConnection;184;2;179;0
WireConnection;338;0;342;0
WireConnection;338;1;341;0
WireConnection;338;2;216;0
WireConnection;177;0;199;0
WireConnection;177;1;211;0
WireConnection;176;3;170;3
WireConnection;169;3;170;2
WireConnection;171;0;184;0
WireConnection;171;1;177;0
WireConnection;171;2;176;0
WireConnection;340;0;338;0
WireConnection;340;1;339;0
WireConnection;340;2;213;0
WireConnection;172;0;199;0
WireConnection;172;1;210;0
WireConnection;159;0;163;0
WireConnection;178;0;171;0
WireConnection;178;1;172;0
WireConnection;178;2;169;0
WireConnection;168;3;170;1
WireConnection;336;0;340;0
WireConnection;336;1;337;0
WireConnection;336;2;165;0
WireConnection;175;0;199;0
WireConnection;175;1;209;0
WireConnection;165;3;159;3
WireConnection;332;0;336;0
WireConnection;332;1;335;0
WireConnection;332;2;158;0
WireConnection;173;0;178;0
WireConnection;173;1;175;0
WireConnection;173;2;168;0
WireConnection;166;0;199;0
WireConnection;166;1;203;0
WireConnection;158;3;159;2
WireConnection;334;0;332;0
WireConnection;334;1;333;0
WireConnection;334;2;157;0
WireConnection;160;0;173;0
WireConnection;160;1;166;0
WireConnection;160;2;165;0
WireConnection;161;0;199;0
WireConnection;161;1;202;0
WireConnection;121;0;130;0
WireConnection;157;3;159;1
WireConnection;327;0;334;0
WireConnection;327;1;329;0
WireConnection;327;2;117;0
WireConnection;164;0;199;0
WireConnection;164;1;201;0
WireConnection;167;0;160;0
WireConnection;167;1;161;0
WireConnection;167;2;158;0
WireConnection;117;3;121;3
WireConnection;331;0;327;0
WireConnection;331;1;326;0
WireConnection;331;2;127;0
WireConnection;126;0;199;0
WireConnection;126;1;197;0
WireConnection;162;0;167;0
WireConnection;162;1;164;0
WireConnection;162;2;157;0
WireConnection;127;3;121;2
WireConnection;120;0;199;0
WireConnection;120;1;196;0
WireConnection;118;0;162;0
WireConnection;118;1;126;0
WireConnection;118;2;117;0
WireConnection;328;0;331;0
WireConnection;328;1;330;0
WireConnection;328;2;123;0
WireConnection;123;3;121;1
WireConnection;144;0;143;0
WireConnection;128;0;118;0
WireConnection;128;1;120;0
WireConnection;128;2;127;0
WireConnection;119;0;199;0
WireConnection;119;1;195;0
WireConnection;321;0;328;0
WireConnection;321;1;323;0
WireConnection;321;2;145;0
WireConnection;145;3;144;3
WireConnection;156;0;199;0
WireConnection;156;1;193;0
WireConnection;122;0;128;0
WireConnection;122;1;119;0
WireConnection;122;2;123;0
WireConnection;325;0;321;0
WireConnection;325;1;320;0
WireConnection;325;2;149;0
WireConnection;149;3;144;2
WireConnection;155;0;199;0
WireConnection;155;1;192;0
WireConnection;322;0;325;0
WireConnection;322;1;324;0
WireConnection;322;2;150;0
WireConnection;148;0;122;0
WireConnection;148;1;156;0
WireConnection;148;2;145;0
WireConnection;150;3;144;1
WireConnection;151;0;148;0
WireConnection;151;1;155;0
WireConnection;151;2;149;0
WireConnection;154;0;199;0
WireConnection;154;1;191;0
WireConnection;37;0;77;0
WireConnection;306;0;322;0
WireConnection;306;1;307;0
WireConnection;306;2;71;0
WireConnection;153;0;151;0
WireConnection;153;1;154;0
WireConnection;153;2;150;0
WireConnection;71;3;37;3
WireConnection;583;0;199;0
WireConnection;583;1;74;0
WireConnection;304;0;306;0
WireConnection;304;1;305;0
WireConnection;304;2;67;0
WireConnection;315;1;317;0
WireConnection;315;2;117;0
WireConnection;67;3;37;2
WireConnection;302;0;304;0
WireConnection;302;1;303;0
WireConnection;302;2;63;0
WireConnection;582;0;199;0
WireConnection;582;1;75;0
WireConnection;73;0;153;0
WireConnection;73;1;583;0
WireConnection;73;2;71;0
WireConnection;319;0;315;0
WireConnection;319;1;314;0
WireConnection;319;2;127;0
WireConnection;9;0;10;4
WireConnection;69;0;73;0
WireConnection;69;1;582;0
WireConnection;69;2;67;0
WireConnection;581;0;199;0
WireConnection;581;1;41;0
WireConnection;63;3;37;1
WireConnection;604;0;302;0
WireConnection;604;1;600;0
WireConnection;604;2;599;0
WireConnection;597;0;596;0
WireConnection;316;0;319;0
WireConnection;316;1;318;0
WireConnection;316;2;123;0
WireConnection;16;0;9;0
WireConnection;17;1;9;0
WireConnection;17;2;25;0
WireConnection;17;3;26;0
WireConnection;17;4;18;1
WireConnection;17;5;18;2
WireConnection;62;0;69;0
WireConnection;62;1;581;0
WireConnection;62;2;63;0
WireConnection;599;3;597;3
WireConnection;605;0;604;0
WireConnection;605;1;602;0
WireConnection;605;2;601;0
WireConnection;609;0;199;0
WireConnection;609;1;603;0
WireConnection;584;1;316;0
WireConnection;574;0;17;0
WireConnection;576;0;16;0
WireConnection;601;3;597;2
WireConnection;611;0;605;0
WireConnection;611;1;607;0
WireConnection;611;2;608;0
WireConnection;612;0;199;0
WireConnection;612;1;606;0
WireConnection;613;0;62;0
WireConnection;613;1;609;0
WireConnection;613;2;599;0
WireConnection;579;0;252;0
WireConnection;593;0;584;0
WireConnection;585;1;611;0
WireConnection;575;0;576;0
WireConnection;573;0;574;0
WireConnection;608;3;597;1
WireConnection;614;0;613;0
WireConnection;614;1;612;0
WireConnection;614;2;601;0
WireConnection;615;0;199;0
WireConnection;615;1;610;0
WireConnection;577;0;575;0
WireConnection;588;0;579;0
WireConnection;578;0;573;0
WireConnection;591;0;585;0
WireConnection;592;0;593;0
WireConnection;616;0;614;0
WireConnection;616;1;615;0
WireConnection;616;2;608;0
WireConnection;594;0;592;0
WireConnection;589;0;591;0
WireConnection;580;0;588;0
WireConnection;64;0;616;0
WireConnection;64;1;578;0
WireConnection;64;2;577;0
WireConnection;428;0;64;0
WireConnection;428;3;594;0
WireConnection;428;4;589;0
WireConnection;428;5;580;0
ASEEND*/
//CHKSM=0315DAA8DC1D1E2F2408AE8B06B7A7C0FCFD8A18