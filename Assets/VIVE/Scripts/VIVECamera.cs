// VIVECamera
// Camera class for rendering an optically distorted scene into the main camera.
// Inspired by Peter Giokaris @ Oculus VR, re-written to make more sense and simplify things for Mocap VR.

using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;

[RequireComponent(typeof(Camera))]

public class VIVECamera : MonoBehaviour
{
	#region Member Variables
	private VIVECameraController CameraController = null;
	public readonly VIVEDistortionMesh distortionMesh = new VIVEDistortionMesh();

	// Relative eye position to head origin, exposed so VIVECameraController can set the IPD.
	[HideInInspector]
	public Vector3 EyePosition  = new Vector3(0.0f, 0.09f, 0.16f);

    // True if this camera corresponds to the right eye.
    public bool RightEye = false;
	#endregion
	
	#region Monobehaviour Member Functions	
	void Awake()
	{
		// Get the VIVECameraController
		CameraController = GetComponentInParent<VIVECameraController>();
		if(CameraController == null)
		{
			Debug.LogWarning("WARNING: VIVECameraController not found!");
			this.enabled = false;
			return;
		}
	}

	void Start()
	{
		// Unity flips the Y render direction when using DirectX.
		var gdVersion = SystemInfo.graphicsDeviceVersion;
		if( gdVersion.Contains("Direct3D") ) {
			flipY = true;
		}
	}
	
	void OnPreCull()
	{
		// Oculus Note: Setting the camera here increases latency, but ensures
		// that all Unity sub-systems that rely on camera location before
		// being set to render are satisfied.
		if(CameraController.CallInPreRender == false)
			SetCameraOrientation();
	}
	
	void OnPreRender()
	{
		// Oculus Note: Better latency performance here, but messes up water rendering and other
		// systems that rely on the camera to be set before PreCull takes place.
		if(CameraController.CallInPreRender == true)
			SetCameraOrientation();
		
		if(CameraController.WireMode == true)
			GL.wireframe = true;
	}
	
	void OnPostRender()
	{
		if(CameraController.WireMode == true)
			GL.wireframe = false;
	}
	#endregion
	
	#region VIVECamera Functions
	void SetCameraOrientation()
	{
		// Right eye camera gets its setCameraOrientation called first, so trigger updating the parent controller's orientation.
		if( RightEye ) {
			CameraController.SetCameraOrientation();
		}

		camera.transform.localRotation = Quaternion.identity;
		camera.transform.localPosition = EyePosition;
	}
	
	///////////////////////////////////////////////////////////
	// PUBLIC FUNCTIONS
	///////////////////////////////////////////////////////////

	public float GetHorizontalFOV()
	{
		float vFOVInRads =  camera.fieldOfView * Mathf.Deg2Rad;
		float hFOVInRads = 2 * Mathf.Atan( Mathf.Tan(vFOVInRads / 2) * camera.aspect);
		float hFOV = hFOVInRads * Mathf.Rad2Deg;
		
		return hFOV;
	}
	
	// Load up the optical distortion mesh for our eye.
	public void LoadEyeMesh()
	{
		distortionMesh.LoadMesh(this, RightEye);
	}
	
	//------ Distortion Mesh & Material ----
	[HideInInspector]
	public Vector2 DistortionScale				= new Vector2(1, 1);
	[HideInInspector]
	public Vector2 DistortionOffset			= new Vector2(0, 0);

	private bool flipY = false;

	public Material lensMaterial;
	public Material GetLenseMaterial()
	{
		if( flipY ) {
			Vector2 screenDistortionScale = new Vector2(DistortionScale.x, DistortionScale.y * -1);
			lensMaterial.SetVector("DistortionScale",  screenDistortionScale);
		} else {
			lensMaterial.SetVector("DistortionScale",  DistortionScale);
		}

		lensMaterial.SetVector("DistortionOffset", DistortionOffset);
		
		return lensMaterial;
	}

	public void Render() {
		distortionMesh.DrawMesh();
	}

	#endregion	
}

//-------------------------------------------------------------------------------------
// ***** VIVECameraGameObject

/// <summary>
/// VIVE camera game object.
/// Used to extend a GameObject for updates within an VIVECamera
/// </summary>
public class VIVECameraGameObject
{
	public GameObject 		   CameraGameObject = null;
	public VIVECameraController CameraController = null;
}

