// VIVEPrefabs
// Quickly add VIVE objects into the scene, by accessing them from the Unity menu bar

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