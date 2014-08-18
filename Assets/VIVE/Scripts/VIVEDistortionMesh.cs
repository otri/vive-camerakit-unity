// Simplified distortion mesh for VIVE system
// August 1, 2014
// @author: Aaron Hilton, David Clement
// Inspired by Peter Giokaris @ Oculus VR, re-written to make more sense and simplify things for Mocap VR.

/*
VIVE - Very Immersive Virtual Experience
Copyright (C) 2014 Aaron Hilton, Emily Carr University

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
*/

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class VIVEDistortionMesh
{	
	[StructLayout(LayoutKind.Sequential)]
	public struct DistMeshVert
	{
		public float	ScreenPosNDC_x;
		public float	ScreenPosNDC_y;
		public float	TimewarpLerp;
		public float	Shade;
		public float	TanEyeAnglesR_u;
		public float	TanEyeAnglesR_v;
		public float	TanEyeAnglesG_u;
		public float	TanEyeAnglesG_v;
		public float	TanEyeAnglesB_u;
		public float	TanEyeAnglesB_v;
	};

	// DistScaleOffsetUV
	[StructLayout(LayoutKind.Sequential)]
	public struct DistScaleOffsetUV
	{
		public float Scale_x;
		public float Scale_y;
		public float Offset_x;
		public float Offset_y;
	};
	
	private Mesh 		mesh = null;

	// Based on Oculus Rift DK1 optical parameters for now.
	public void GetIdealResolution(ref int w, ref int h)
	{
		w=1280; h=800;
	}

	public void LoadMesh(VIVECamera camera, bool rightEye)
	{
		// Only load once.
		if(mesh) return; 

		mesh = new Mesh ();
		mesh.MarkDynamic();

		Vector3[] 	mesh_verts = null;
		Vector2[] 	mesh_uv = null; 		// TEXCOORD0
		Vector2[] 	mesh_uv2 = null; 		// TEXCOORD1
		Vector3[] 	mesh_normal = null;		// NORMALS
		int[] 		mesh_triIndices = null;

		int numVerts;
		int numIndicies;
		string lenseFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, rightEye ? "VIVELenses/right.lens" : "VIVELenses/left.lens");
		using ( StreamReader reader = new StreamReader( lenseFilePath ) )
		{
			// Exactly the reverse of the serialation code.
			numVerts = int.Parse(reader.ReadLine());

			mesh_verts = new Vector3[numVerts];
			mesh_uv = new Vector2[numVerts];
			mesh_uv2 = new Vector2[numVerts];
			mesh_normal = new Vector3[numVerts];

			for ( int i = 0; i < numVerts; i++ )
			{
				string[] fragments = reader.ReadLine().Split( new char[] { ' ' } );
				mesh_verts[i].x = float.Parse(fragments[0]);
				mesh_verts[i].y = float.Parse(fragments[1]);
				mesh_verts[i].z = float.Parse(fragments[2]);
				mesh_uv[i].x = float.Parse(fragments[3]); // Load RED cromatic distortion
				mesh_uv[i].y = float.Parse(fragments[4]);
				mesh_uv2[i].x = float.Parse(fragments[5]); // Load GREEN chromatic distortion
				mesh_uv2[i].y = float.Parse(fragments[6]);
				mesh_normal[i].x = float.Parse(fragments[7]); // Load BLUE chromatic distortion
				mesh_normal[i].y = float.Parse(fragments[8]);
			}

			numIndicies = int.Parse(reader.ReadLine());
			mesh_triIndices = new int[numIndicies];
			for ( int i = 0; i < numIndicies; i+=3 )
			{
				string[] fragments = reader.ReadLine().Split( new char[] { ' ' } );
				mesh_triIndices[i+0] = int.Parse(fragments[0]);
				mesh_triIndices[i+1] = int.Parse(fragments[1]);
				mesh_triIndices[i+2] = int.Parse(fragments[2]);
			}
			{
				string[] scaleFragments = reader.ReadLine().Split( new char[] { ' ' } );
				camera.DistortionScale.x  = float.Parse( scaleFragments[0] );
				camera.DistortionScale.y  = float.Parse( scaleFragments[1] );
				camera.DistortionOffset.x = float.Parse( scaleFragments[2] );
				camera.DistortionOffset.y = float.Parse( scaleFragments[3] );
			}
		}

		mesh.vertices  = mesh_verts;
		mesh.uv        = mesh_uv;
		mesh.uv2       = mesh_uv2;
		mesh.normals   = mesh_normal;
		mesh.triangles = mesh_triIndices;
		mesh.Optimize();
	}
	
	public void DrawMesh()
	{
		if(mesh == null)
			return;

		Graphics.DrawMeshNow(mesh, Matrix4x4.identity);		
	}
}