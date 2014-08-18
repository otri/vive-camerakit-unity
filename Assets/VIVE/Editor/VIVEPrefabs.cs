// VIVEPrefabs
// Quickly add VIVE objects into the scene, by accessing them from the Unity menu bar

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
using System.Collections;
using UnityEditor;

class VIVEPrefabs
{
	[MenuItem ("VIVE/Prefabs/VIVECameraController")]	
	static void CreateVIVECameraController ()
	{
		Object obj = AssetDatabase.LoadAssetAtPath ("Assets/VIVE/Prefabs/VIVECameraController.prefab", typeof(UnityEngine.Object));
		PrefabUtility.InstantiatePrefab(obj);
    }	
	
	[MenuItem ("VIVE/Prefabs/VIVECompass")]	
	static void CreateVIVECompass ()
	{
		Object obj = AssetDatabase.LoadAssetAtPath ("Assets/VIVE/Prefabs/VIVECompass.prefab", typeof(UnityEngine.Object));
		PrefabUtility.InstantiatePrefab(obj);
    }	
}