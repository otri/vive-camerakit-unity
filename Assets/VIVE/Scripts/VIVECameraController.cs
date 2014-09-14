// VIVECameraController
// Camera Controller for setting up the IPD of the cameras, and tracking with the mocap "head" input.
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
using System.Collections.Generic;

public class VIVECameraController : MonoBehaviour
{		
	// PRIVATE MEMBERS
	private bool   UpdateCamerasDirtyFlag = false;
	private Camera CameraLeft, CameraRight = null;
	private GameObject MocapHeadset = null;
	private float  AspectRatio = 1.0f;

	// IPD
	[SerializeField]
	private float  		ipd 		= 0.064f; 				// in millimeters
	public 	float 		IPD
	{
		get{return ipd;}
		set{ipd = value; UpdateCamerasDirtyFlag = true;}
	}
	
	// VERTICAL FOV
	[SerializeField]
	private float  		verticalFOV = 90.0f;	 			// in degrees
	public 	float		VerticalFOV
	{
		get{return verticalFOV;}
		set
		{
			verticalFOV = Mathf.Clamp(value, 45.0f, 140.0f);
			UpdateCamerasDirtyFlag = true;
		}
	}
	

	// Camera positioning:
	// Set this transform with an object that the camera orientation should follow.
	[SerializeField]
	private GameObject 	vroamCompass = null;
	public GameObject 	VRoamCompass {
		get{return  vroamCompass;}
		set
		{
			vroamCompass = value;
			UpdateCamerasDirtyFlag = true;
		}
	}
	// Use this to decide where tracker sampling should take place
	// Setting to true allows for better latency, but some systems
	// (such as Pro water) will break
	public bool			CallInPreRender = false;
	// Use this to turn on wire-mode
	public bool			WireMode  		= false;
	public bool			ShowDistortionWire 	= false;

	// UNITY CAMERA FIELDS
	// Set the background color for both cameras
	[SerializeField]
	private Color 		backgroundColor = new Color(0.192f, 0.302f, 0.475f, 1.0f);
	public  Color       BackgroundColor
	{
		get{return backgroundColor;}
		set{backgroundColor = value; UpdateCamerasDirtyFlag = true;}
	}
	// Set the near and far clip plane for both cameras
	[SerializeField]
	private float 		nearClipPlane   = 0.05f;
	public  float 		NearClipPlane
	{
		get{return nearClipPlane;}
		set{nearClipPlane = value; UpdateCamerasDirtyFlag = true;}
	}
	[SerializeField]
	private float 		farClipPlane    = 1000.0f;  
	public  float 		FarClipPlane
	{
		get{return farClipPlane;}
		set{farClipPlane = value; UpdateCamerasDirtyFlag = true;}
	}
	
	// * * * * * * * * * * * * *
		
	/// <summary>
	/// Awake this instance.
	/// </summary>
	void Awake()
	{
		// Create a Mocap "oculus" camera target for reference.
		// Using oculus for historical reasons.
		MocapHeadset = new GameObject("oculus");
		MocapHeadset.tag = "Mocap";

		// Get the cameras
		Camera[] cameras = gameObject.GetComponentsInChildren<Camera>();
		
		for (int i = 0; i < cameras.Length; i++)
		{
			if(cameras[i].name == "CameraLeft")
				CameraLeft = cameras[i];
			
			if(cameras[i].name == "CameraRight")
				CameraRight = cameras[i];
		}

		if(CameraLeft == null)
			Debug.LogWarning("VIVECameraController: No CameraLeft found!");
		if(CameraRight == null)
			Debug.LogWarning("VIVECameraController: No CameraRight found!");
	}

	void Start()
	{
		// Make sure we have an origin compass in the scene.
		if( VRoamCompass == null ) {
			var originCompass = new GameObject("Origin Compass");
			VRoamCompass = originCompass;
		}

		// Initialize the cameras
		UpdateCamerasDirtyFlag = true;
		Update();

		// Keep render quality ultra-low latency, no vsync.
		QualitySettings.maxQueuedFrames = 		0;
		QualitySettings.vSyncCount = 			0;

		RenderStart();
	}

	/// <summary>
	/// Sets the camera orientation, combining MocapOculus data with the selected VRoamCompass.
	/// </summary>
	public void SetCameraOrientation()
	{
		// MocapSocket pre-applies the VRoamCompass to the MocapHeadset transform, as it does to all Mocap tagged objects.
		camera.transform.rotation = MocapHeadset.transform.rotation;
		camera.transform.position = MocapHeadset.transform.position;
	}

	/// <summary>
	/// Inits the camera controller variables.
	/// Made public so that it can be called by classes that require information about the
	/// camera to be present when initing variables in 'Start'
	/// </summary>
	public void InitCameraControllerVariables()
	{
		VerticalFOV = 90;
		AspectRatio = 1;
	}

	/// <summary>
	/// Check and update the camera configuration if dirty.
	/// </summary>
	void Update()
	{
		// Handle all other camera updates here
		if(UpdateCamerasDirtyFlag == false) return;
		UpdateCamerasDirtyFlag = false;

		// Configure left and right cameras
		float eyePositionOffset = -IPD * 0.5f;
		ConfigureCamera(CameraLeft, eyePositionOffset);

		eyePositionOffset       = IPD * 0.5f;
		ConfigureCamera(CameraRight, eyePositionOffset);

		// Make sure the mocap transform is set.
		if( CameraRight ) {
			MocapSocket mocap = CameraRight.GetComponent<MocapSocket>();
			mocap.VRoamTransform = VRoamCompass.transform;
		}
	}

	bool ConfigureCamera(Camera camera, float eyePositionOffset)
	{				
		// Always set camera fov and aspect ratio
		camera.fieldOfView = VerticalFOV;
		camera.aspect      = AspectRatio;
			
		// Push params also into the mesh distortion instance (if there is one)
		camera.GetComponent<VIVECamera>().LoadEyeMesh();
					
		// Set camera variables that pertain to the eye position
		Vector3 EyePosition = Vector3.zero;
		EyePosition.x = eyePositionOffset; 
		camera.GetComponent<VIVECamera>().EyePosition = EyePosition;		
					
		// Background color
		camera.backgroundColor = BackgroundColor;
		
		// Clip Planes
		camera.nearClipPlane = NearClipPlane;
		camera.farClipPlane = FarClipPlane;
			
		return true;
	}
	
	// Public Accessors to inner cameras
	public Transform GetCameraTransform()
	{
		return CameraRight.transform;
	}
	
	//------ Camera Rendering Stack  ------

	// Prepare for rendering.
	void RenderStart()
	{
		if(CameraLeft == null || CameraRight == null)
		{
			Debug.LogError("No left & right cameras!");
			return;
		}

		// Without this, we will be drawing 1 frame behind
		camera.depth = Mathf.Max (CameraLeft.depth, CameraRight.depth) + 1;
		
		// Don't want the camera to render anything..
		camera.cullingMask = 0;
		camera.eventMask = 0;
		camera.useOcclusionCulling = false;
		camera.backgroundColor = Color.black;
		camera.clearFlags = CameraClearFlags.Nothing;
		camera.renderingPath = RenderingPath.Forward;
		camera.orthographic = true;

		// Setup left/right camera rendering target regions
		CameraLeft.camera.rect = new Rect(0f, 0, 0.5f, 1);
		CameraRight.camera.rect = new Rect(0.5f, 0, 0.5f, 1);
	}

	/// <summary>
	/// Raises the render image event.
	/// </summary>
	/// <param name="source">Source.</param>
	/// <param name="destination">Destination.</param>
	void  OnRenderImage(RenderTexture source, RenderTexture destination)
	{	
		if(CameraLeft == null || CameraRight == null)
		{
			Debug.LogError("No left & right cameras!");
			return;
		}
		
		// Make the destination texture the target for all rendering
		RenderTexture.active = destination;

		// Clear the destination
		GL.Clear (false, true, Color.black);

		// Render both eyes.
		RenderEye(false, source);
		RenderEye(true, source);
		
		// Flush GL output.
		GL.IssuePluginEvent(1);
	}

	// Directly render the eye's camera onto the correct portion of the render target.
	void RenderEye(bool rightEye, RenderTexture source)
	{
		Camera activeCam = (rightEye) ? CameraRight : CameraLeft;
		var viveCam = activeCam.GetComponent<VIVECamera>();
		Material material = viveCam.GetLenseMaterial();
		
		// Assign the source texture to a property from a shader
		material.mainTexture = source;
		
		float halfWidth = 0.5f * Screen.width;
		GL.Viewport(new Rect(rightEye ? halfWidth : 0f, 0f, halfWidth, Screen.height));
		
		if(ShowDistortionWire)
			GL.wireframe = true;
		
		// Set up the simple Matrix
		GL.PushMatrix ();
		GL.LoadOrtho ();
		for(int i = 0; i < material.passCount; i++)
		{
			material.SetPass(i);
			viveCam.Render();
		}
		GL.PopMatrix ();
		
		if(ShowDistortionWire)
			GL.wireframe = false;
	}
}

