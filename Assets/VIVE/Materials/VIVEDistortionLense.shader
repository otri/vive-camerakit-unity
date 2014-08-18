// VIVEDistortionLense.shader
// This is the full screen shader program to warp camera image onto the HMD.
// Inspired by Peter Giokaris @ Oculus VR for writing the original shader.
//  Re-written to make more sense and simplify things.

//VIVE - Very Immersive Virtual Experience
//Copyright (C) 2014 Aaron Hilton, Emily Carr University
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA


Shader "VIVE/VIVEDistortionLense"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "" {}
	}
	
	// Shader code pasted into all further CGPROGRAM blocks
	CGINCLUDE
	#include "UnityCG.cginc"
	
	struct vertex_stream
	{
    	float4 pos      : POSITION;
    	float2 uv       : TEXCOORD0; // RED distortion UVs
    	float2 uv2      : TEXCOORD1; // GREEN distortion UVs
    	float3 norm		: NORMAL;  // Hacked in BLUE distortion UVs in the normal coordinate
	};
	
	struct fragment_stream 
	{
		float4 pos 		: POSITION; // Encoding the x,y, and z is fall-off around the edges
		float2 uv 		: TEXCOORD0; // Incoming frag RED distortion
		float2 uv2 		: TEXCOORD1; // Incoming frag GREEN distortion
		float2 norm		: TEXCOORD2; // Incoming frag BLUE distortion in the normal vector
		float4 c		: COLOR;     // Final colour output.
	};
	
	sampler2D _MainTex;

	// Distortion parameters get set-up per eye lense.
	float2 DistortionScale  = float2(0,0);
	float2 DistortionOffset = float2(0,0);		
				
	fragment_stream vert( vertex_stream v )
	{
		fragment_stream o;
		
		o.pos = v.pos;
		o.uv = v.uv.xy * DistortionScale + DistortionOffset;
		o.uv2 = v.uv2.xy * DistortionScale + DistortionOffset;
		o.norm = v.norm.xy * DistortionScale + DistortionOffset;
		o.c   = o.pos.z;

		return o;
	} 
		
	float4 frag(fragment_stream i) : COLOR 
	{
		float red   = tex2D (_MainTex, i.uv).x;
		float green = tex2D (_MainTex, i.uv2).y;    
    	float blue  = tex2D (_MainTex, i.norm).z;
    	float alpha = 1;
    	
//    	// Oculus Note: This is required to get multi-sampling in frag shader to work properly
//    	if (any(clamp(i.uv2, float2(0, 0), float2(1, 1)) - i.uv2))
//    	{
//        	red   = 0;
//    		green = 0;    
//    		blue  = 0;
//		}
    	
    	//return i.c;
    	return float4(red, green, blue, alpha) * i.c;
	}

	ENDCG 
	
	Subshader 
	{
 	Pass 
 	{
	 	ZTest Always Cull Off ZWrite Off
	  	Fog { Mode off }      

      	CGPROGRAM
      	//#pragma fragmentoption ARB_precision_hint_nicest
      	#pragma vertex vert
      	#pragma fragment frag
      	ENDCG
  	}
}

Fallback off
	
} // shader