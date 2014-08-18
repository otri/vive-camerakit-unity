// VIVECameraControllerEditor
// Show only the set of parameters we want, with pretty headings and separators.
//
// Inspired by Peter Giokaris @ Oculus VR, re-written to make more sense and simplify things for Mocap VR.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(VIVECameraController))]
public class VIVECameraControllerEditor : Editor
{
	private VIVECameraController _component;

	void OnEnable() {
		_component = (VIVECameraController)target;
	}

	public static void Separator()
	{
		GUI.color = new Color(1, 1, 1, 0.25f);
		GUILayout.Box("", "HorizontalSlider", GUILayout.Height(16));
		GUI.color = Color.white;
	}

	// OnInspectorGUI
	public override void OnInspectorGUI()
	{
		Undo.RecordObject(_component, "VIVECameraController");

		_component.VRoamCompass 	= EditorGUILayout.ObjectField("Starting Compass",
		                                                          _component.VRoamCompass,
		                                                          typeof(Transform), true) as Transform;
		Separator();			
		
		_component.VerticalFOV 			= EditorGUILayout.FloatField("Vertical FOV", _component.VerticalFOV);
		_component.IPD 					= EditorGUILayout.FloatField("IPD", _component.IPD);
		
		Separator();

		_component.CallInPreRender 		= EditorGUILayout.Toggle ("Call in Pre-Render", _component.CallInPreRender);
		_component.WireMode 			= EditorGUILayout.Toggle ("Wire-Frame Mode", _component.WireMode);
		_component.ShowDistortionWire 	= EditorGUILayout.Toggle ("Show Distortion Mesh", _component.ShowDistortionWire);

		Separator();
	
		_component.BackgroundColor 		= EditorGUILayout.ColorField("Background Color", _component.BackgroundColor);
		_component.NearClipPlane       	= EditorGUILayout.FloatField("Near Clip Plane", _component.NearClipPlane);
		_component.FarClipPlane        	= EditorGUILayout.FloatField("Far Clip Plane", _component.FarClipPlane);			
		
		Separator();

		if(GUI.changed)
		{
			EditorUtility.SetDirty(_component);
		}
	}
}

