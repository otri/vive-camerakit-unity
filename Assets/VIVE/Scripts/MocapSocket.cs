/*
VIVE - Very Immersive Virtual Experience
Copyright (C) 2014 Alastair Macleod, Emily Carr University

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
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;

using System.Runtime.InteropServices;

class SegmentItem
{
	public SegmentItem(string _name, float[] _tr, float[] _ro, bool _isJoint)
	{
		name = _name;
		isJoint = _isJoint;
		tr = new float[3];
		tr = _tr;
		ro = new float[4];
		ro = _ro;
	}

	public string name;
	public float[] tr;
	public float[] ro;
	public bool isJoint;

};

public class MocapSocket : MonoBehaviour {
//
//	[DllImport("UnityDllTest",  EntryPoint = "getData", CallingConvention = CallingConvention.Cdecl) ]
//	private static extern int getData(byte[] data, int len);
//
//	[DllImport("UnityDllTest") ]
//	private static extern int test();
//
//	[DllImport("UnityDllTest") ]
//	private static extern bool connect();
//
//	[DllImport("UnityDllTest") ]
//	private static extern bool disconnect();
//
//	[DllImport("UnityDllTest") ]
//	private static extern bool isConnected();
//
//	[DllImport("UnityDllTest") ]
//	private static extern int peek();
	
	private Socket clientSocket = null;
	private string buffer;
	private string infoMessage = "";
	private bool   isActive = false;

	const string BODY_OBJECT       = "BetaVicon";
	const string OCULUS_OBJECT     = "Oculus";
	const string LEFT_HAND_OBJECT  = "LeftHandRigid";
	const string RIGHT_HAND_OBJECT = "RightHandRigid";
	const string LEFT_FOOT_OBJECT  = "LeftFootRigid";
	const string RIGHT_FOOT_OBJECT = "RightFootRigid";
	const string TEST_OBJECT       = "TEST";


	public string      hostIp        = "127.0.0.1";
	public int         port          = 4001;
	public bool        lockNavigator = true;  // Set to true to disable the camera when connected
	public float       markerSize    = 0.3f;  // Size of markers when displayed
	public float       worldScale = 1f;
	private Boolean    toggleDisplay = false; // Show debug text when true
	private Boolean    toggleHelp    = false; // Show help scren
	private Boolean    showMarkers   = false; // Show markers when true
	private GameObject markerGroup;
	public string      prefix        = "";

	List<GameObject> skeletonObjects = new List<GameObject>();
	List<GameObject> rigidObjects = new List<GameObject>();
	Dictionary< string, GameObject >     objectDict         = new Dictionary<string, GameObject> ();
	Dictionary< GameObject, Quaternion > rotationOffsets    = new Dictionary<GameObject, Quaternion> ();
	Dictionary< GameObject, Quaternion > initialOrientation = new Dictionary<GameObject, Quaternion> ();
	Dictionary< string, GameObject >     markerDict         = new Dictionary< string, GameObject >();

	public Transform VRoamTransform = null;

	int frameCount    = 0;
	double dt         = 0.0;
	double fps        = 0.0;
	double updateRate = 4.0;  // 4 updates per sec.

	// These are special flags that are set to trigger the reseting of the orientation for various objects in the scene.
	// Typically they are false however, when an operator presses the appropriate key, the current orientation of the object
	// is retrieved and set as the base orientation.
	// The logic works as follows:
	// - The flag is false
	// - If an operator presses a given key, this is intercepted in the OnGUI callback.  Since this callback
	//   does not have the current state of motion capture information it simply sets the flag in the hope it will 
	//   be handled in ProcessFrame.
	// In ProcessFrame, each frame of information being emmited by the motion capture system is parsed and each rigid body definition is
	// decoded to get its translation and orientation.
	// If the flag is set to true, the inverse of the current orientation is set as the new base orientation so that when the current orientation is applied
	// it is nullified by the inverse orientation to produce the correct orientation.
	bool captureOriOculus = false;
	bool captureOriBody   = false;
	bool captureOriHands  = false;
	bool captureOriFeet   = false;


	void OnGUI()
	{

		int wid1 = 70;
		int wid2 = 120;
		int wid3 = 120;
		int x1 = 5;
		int x2 = x1 + wid1 + 5;
		int x3 = x2 + wid2 + 5;
		int y = 0;
		int rowh = 23;

		// Display Things

		GUI.skin.GetStyle ("Label").alignment = TextAnchor.UpperLeft;

		if(Time.fixedTime < 2)
		{
			GUIStyle st = new GUIStyle(GUI.skin.GetStyle("Label"));
			st.alignment = TextAnchor.MiddleCenter;
			int w = 200;
			GUI.Label (new Rect (Screen.width/2 - w/2, Screen.height * 0.382f, w, 40), "Press P for help", st);
		}


		if(toggleDisplay)
		{
			if (isActive) 
			{
				String infoFps = "Connected " + fps.ToString ("0.00") + " fps\n";
				GUI.Label (new Rect (Screen.width - 150, 5, 150, 40), infoFps);
			}
			else
			{
				GUI.Label (new Rect (Screen.width - 150, 5, 150, 40), "Not Connected");
			}

			y+=10;

			// Rows
			GUI.Label(	            new Rect(x1, y, wid1, rowh), "Host:");
			GUI.SetNextControlName("IPField");
			hostIp = GUI.TextField (new Rect(x2, y, wid2, rowh), hostIp, rowh);

			y+=25;

			GUI.Label(	            new Rect(x1, y, wid1, rowh), "Port:");
			GUI.SetNextControlName("PortField");
			port = int.Parse (GUI.TextField (new Rect(x2, y, wid2, rowh), port.ToString(), rowh));
			if(isActive)
			{
				if(GUI.Button ( new Rect(x3	, y,  wid3, rowh), "Disconnect")) DisconnectStream();
			}
			else
			{
				if( GUI.Button ( new Rect(x3	, y,  wid3, rowh), "Connect")) ConnectStream	();
			}

			y+=25;

			GUI.Label(	            new Rect(x1, y, wid1, rowh), "Prefix:");
			GUI.SetNextControlName("PrefixField");
			prefix = GUI.TextField (new Rect(x2, y, wid2, rowh), prefix, rowh);
			y+=25;

			GUI.Label(	new Rect(x1, y, Screen.width-10, Screen.height-y-5), infoMessage);

//			MouseNavigator.topCrop = y;
		}

		if(toggleHelp)
		{
			String text = "VIVE - Very Imersive Virtual Experience\n";
			text += "Alastair Macleod, Emily Carr University, The Sawmill\n";
			text += "Version 0.1\n";
			text += "1 - Reset Oculus\n";
			text += "B - Zero Body\n";
			text += "H - Zero Hands\n";
			text += "F - Zero Feet\n";
			text += "M - Show Markers\n";
			text += "Y - Debug Data\n";
			text += "C - Connect\n";
			text += "D - Disconnect\n";
			GUI.Label ( new Rect(5, 5, 	Screen.width, Screen.height), text );
		}


		// Don't check keys if a field is activated

		if (GUI.GetNameOfFocusedControl ().Equals ("PrefixField")) return;
		if (GUI.GetNameOfFocusedControl ().Equals ("IPField")) return;
		if (GUI.GetNameOfFocusedControl ().Equals ("PortField")) return;


		// Key events

		Event e = Event.current;
		
		if (e.type == EventType.KeyUp)
		{

			if(e.keyCode == KeyCode.X) Application.LoadLevelAsync ("Entrance");
			if(e.keyCode == KeyCode.C) ConnectStream();

			if(isActive)
			{
				if(e.keyCode == KeyCode.Alpha1) captureOriOculus  = true;
				if(e.keyCode == KeyCode.B)      captureOriBody    = true;
				if(e.keyCode == KeyCode.H)      captureOriHands   = true;
				if(e.keyCode == KeyCode.F)      captureOriFeet    = true;
				if(e.keyCode == KeyCode.D && clientSocket.Connected) DisconnectStream();
			}

			if(e.keyCode == KeyCode.M)
			{
				if(showMarkers)
				{
					foreach(string sMarker in markerDict.Keys)
					{
						Destroy(markerDict[sMarker]);
					}
					markerDict.Clear ();
					showMarkers = false;
				}
				else
				{
					showMarkers = true;
				}
			}

			if(e.keyCode == KeyCode.P)
			{
				toggleDisplay = false;
				toggleHelp = !toggleHelp;
			}

			if(e.keyCode == KeyCode.Y)
			{
				toggleHelp = false;
				toggleDisplay = !toggleDisplay;
			}
		}
	}
	
	void SetInitialState(GameObject o)
	{
		objectDict[o.name.ToLower ()] = o;
		rotationOffsets.Add (o, Quaternion.identity);
		initialOrientation.Add (o, o.transform.localRotation);
	}
		
	// Use this for initialization
	void Start ()
	{
		toggleDisplay = false;
		infoMessage   = "INIT";

		string[] skeletonList =  new string[] {"Hips",  
			"LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", 
			"RightUpLeg", "RightLeg", "RightFoot", "RightToeBase", 
			"Spine", "Spine1", "Spine2", "Spine3",
			"Neck", "Neck1", "Head",     
			"LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", 
			"RightShoulder", "RightArm", "RightForeArm", "RightHand",
			BODY_OBJECT
		};

		try {
			GameObject[] mocapTagged = GameObject.FindGameObjectsWithTag ("Mocap");

			if(mocapTagged != null)
			{
				foreach (GameObject o in mocapTagged)
				{
					Debug.Log ("Adding Rigid Object: " + o.name);
					rigidObjects.Add (o);
					SetInitialState(o);
					if(o.name.ToLower() != OCULUS_OBJECT.ToLower()) o.SetActive (false);
				}
			}
		} catch( UnityException ) {}

//		// Automatically attach Oculus to the VIVECameraController object
//		GameObject oculusTarget = GameObject.FindObjectOfType<VIVECameraController>().gameObject;
//		oculusTarget.name = "oculus";
//		rigidObjects.Add(oculusTarget);
//		SetInitialState(oculusTarget);

		foreach(string s in skeletonList) 
		{
			GameObject o = GameObject.Find(s);
			if (o == null) continue;
			skeletonObjects.Add (o);
			SetInitialState(o);

			//if(s == BODY_OBJECT) o.SetActive (false);
		}

		markerGroup = new GameObject ("MOCAP_MARKERS");

		ConnectStream();
	}

	void ConnectStream()
	{
		if( clientSocket != null )
			return;

		Debug.Log("Connecting to network socket");
		IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(hostIp), port);
		AddressFamily family  = AddressFamily.InterNetwork;
		SocketType    sokType = SocketType.Stream;
		ProtocolType  proType = ProtocolType.Tcp;
		clientSocket = new Socket(family, sokType, proType);

		try
		{
			clientSocket.Connect(ipEndPoint);
			if(clientSocket.Connected == false) return;
			clientSocket.Blocking = false;
		}
		catch(SocketException)
		{
			clientSocket.Close();
			clientSocket = null;
			//Debug.Log ("Socket Exception: " + e.ToString ());
			return;
		}

		isActive = true;
	}

	void DisconnectStream()
	{
		isActive = false;
		if (!clientSocket.Connected) return;
		clientSocket.Disconnect (false);
		clientSocket.Close();
		clientSocket = null;
	}
	
	void TimeClick()
	{
		dt += Time.deltaTime;
		if (dt > 1.0/updateRate)
		{
			fps = frameCount / dt ;
			frameCount = 0;
			dt -= 1.0/updateRate;
		}
	}
	
	// Get data from a TCP socket
	bool GetSocketData(out string outData)
	{
		outData = "";

		if (clientSocket == null || !clientSocket.Connected)
		{
			infoMessage += "Not Connected while trying to get packet\n";
			return false;
		}

		try
		{
			// Get some data, append to current buffer
			byte[] socketData = new byte[1024*16];
			int ret = clientSocket.Receive (socketData, 1024*16, 0);
			buffer += Encoding.ASCII.GetString(socketData, 0, ret);
						
			// find split point
			int splitHere = buffer.LastIndexOf ("END\r\n");
			if(splitHere == -1) return false;
			
			// everything after last split point gets buffered
			string extracted = buffer.Substring(0, splitHere);
			if(buffer.Length == splitHere + 5)
				buffer = "";
			else
				buffer = buffer.Substring (splitHere+5);
			
			// split packets
			string[] splitsPackets = { "END\r\n" };
			string[] packetItems = extracted.Split(splitsPackets, StringSplitOptions.RemoveEmptyEntries);
			if(packetItems.Length == 0) return false;

			outData = packetItems[packetItems.Length -1];
			return true;
		}
		catch(SocketException e)
		{
			if(e.ErrorCode == 10035) return false;
			infoMessage += "Socket Error: " + e.ToString() + " (" + e.ErrorCode + ")\n";
		}

		return false;
	}

	// Get data, parse and return as a subject list
	bool GetData(out Dictionary< string, SegmentItem[] > subjectList)
	{
		subjectList = new Dictionary< string, SegmentItem[] > ();
		string packet = "";

		if( GetSocketData( out packet) == false ) {
			return false;  // Early exit, with no data.
		}
			
		string[] lineItems = packet.Split ('\n');
		if(lineItems.Length == 0)
		{
			infoMessage += "No data error\n";
			return false;
		}
			
		int subjects = 0;

		if(!int.TryParse ( lineItems[0], out subjects))
		{
			infoMessage += "Invalid data while parsing subjects\n";
			return false;
		}

		int line = 1;

		for(int i=0; i < subjects && line < lineItems.Length; i++)
		{
			string[] subjectSplit = lineItems[line++].Split ('\t');
			string subjectName = subjectSplit[0].ToLower ();
			// Strip prefix
			if(prefix.Length > 0 && prefix.Length < subjectName.Length)
			{
				if(subjectName.Substring(0, prefix.Length) == prefix.ToLower ())
				{
					subjectName = subjectName.Substring (prefix.Length);
				}
			}
			int    noSegments  = Convert.ToInt32 (subjectSplit[1]);
			int    noMarkers   = Convert.ToInt32 (subjectSplit[2]);
			infoMessage += subjectName + "  " + noSegments + " segments   " + noMarkers + " markers\n";

			SegmentItem[] items = new SegmentItem[noSegments + noMarkers];

			// Segments	
			int item_i=0;
			for(int j=0; j < noSegments && line < lineItems.Length; j++)
			{
				string[] segmentSplit = lineItems[line++].Split('\t');
					
				if(segmentSplit.Length != 8)
				{
					infoMessage += "Segment Error: " + segmentSplit.Length;
					continue;
				}
				float[] tr = new float[3];
				float[] ro = new float[4];
				for(int k=0; k < 3; k++) tr[k] = float.Parse (segmentSplit[k+1]);
				for(int k=0; k < 4; k++) ro[k] = float.Parse (segmentSplit[k+4]);
				items[item_i++] = new SegmentItem(segmentSplit[0], tr, ro, true);
			}

			float[] zero = new float[4];
			for(int j=0; j < 4; j++) zero[j] = 0.0f;


			// Markers
			for(int j=0; j < noMarkers && line < lineItems.Length; j++)
			{
				string[] segmentSplit = lineItems[line++].Split('\t');
				
				if(segmentSplit.Length != 4)
				{
					Debug.Log ("Marker Error" + segmentSplit.Length);
					infoMessage += "Marker Error: " + segmentSplit.Length;
					continue;
				}
				float[] tr = new float[3];
				for(int k=0; k < 3; k++)
				{
					if ( segmentSplit[k+1] == "nan" )
					{
						tr[k] = float.NaN;
					}
					else if ( float.TryParse(segmentSplit[k+1], out tr[k]) == false )
					{
						Debug.LogWarning( "Could not parse line: " + lineItems[line-1] );
					}
				}
				items[item_i++] = new SegmentItem(segmentSplit[0], tr, zero, false);
			}
			if(!subjectList.ContainsKey(subjectName))
				subjectList.Add (subjectName, items);
		}

		return true;
	}
	
	// Update is called once per frame
	// Execute the update of frame data as early as possible.
	// http://docs.unity3d.com/Manual/ExecutionOrder.html
	void OnPreCull()
	{
		processFrame();
	}

	public void processFrame()
	{
		// If no mocap data stream is currently connected, exit.
		if (!isActive)
		{
			return;
		}
		// If the socket connection is no longer available, exit.
		if( clientSocket == null)
		{
			infoMessage = "NO CONNECTION";
			return;
		}

		Dictionary< string, SegmentItem[] > subjectList;

		// Extract a list of subjects from the connected motion capture data stream.  If this fails then exit.
		if(!GetData(out subjectList)) { return; }

		// Frame rate calculator
		TimeClick ();

		infoMessage = "Subjects: " + subjectList.Keys.Count + "\n";

		// The motion capture stream consists of a sequence of frames.  Each frame contains information on zero or more
		// named subjects (rigid bodies or skeletons) currently being tracked.
		// Each subject has a set of information associated with it.
		// In the case of rigid bodies this consists of a "root" object which represents the best fit centroid of the
		// marker locations associated with the subject followed by the raw positions of each of the visible markers 
		// that make up that rigid body.  The motion capture system is currently responsible for matching
		// the relative positions of markers to a given rigid body subject.  Once it has done this matching it can infer
		// an averaged position and orientation of the rigid body which is stored in the root.
		// Thus, for each subject, extract its name, the root translation and orientation.
		// The marker locations are also available if they need to be rendered but this is not currently used.

		// For each subject extracted from the motion capture data stream...
		foreach(KeyValuePair<String, SegmentItem[]> entry in subjectList)
		{
			// Extract the subject's name
			string subject = entry.Key.ToLower();

			infoMessage += "SUBJECT: " + subject;

			bool frameDone = false;


			// For each segment in subject...
			foreach(SegmentItem item in entry.Value)
			{
				// If an item is null it is most likely not currently being tracked by the motion capture system.
				// For example an occuluded marker or a rigid body that is outside the volume.
				if(item == null)
				{
					infoMessage += " - skipping null\n";
					continue;
				}

				// If the item is a "joint" this simply means that it has both position and orientation.
				// Rigid bodies and skeleton "bones" are both joints.
				if(item.isJoint)
				{
					// Extract the current orientation of the joint from the item.  This orientation is stored in a
					// quaternion.  http://en.wikipedia.org/wiki/Quaternion
					Quaternion localOrientation  =  new Quaternion(item.ro[0], item.ro[1], item.ro[2], item.ro[3]);

					// The game object is an object within Unity that is associated with a Joint in the mocap system.
					GameObject o = null;

					// Determine if the subject is one of a special set of objects which can have their orientation
					// manually reset at runtime.  These subjects must be rigid bodies (not skeleton "bones".
					bool isHands  = (subject == LEFT_HAND_OBJECT.ToLower()  || subject == RIGHT_HAND_OBJECT.ToLower());
					bool isTest   = (subject == TEST_OBJECT.ToLower() );
					bool isFeet   = (subject == RIGHT_FOOT_OBJECT.ToLower() || subject == LEFT_FOOT_OBJECT.ToLower());
					bool isOculus = (subject == OCULUS_OBJECT.ToLower());


					// If the object is a resetable rigid body...
					if ( isHands || isFeet || isOculus || isTest )
					{
						// If this item is the "root"
						// Rigidbody joint
						if(item.name == "root")
						{
							// Rigid bodies match on subject name, not on the segment name.  If we can't find
							// an object in the Unity scene graph that matches the subject name, continue onto the next object.
							if ( !objectDict.ContainsKey( subject.ToLower() ) )
							{
								infoMessage += "- Rigid Missing\n";
								continue;
							}
							infoMessage += " (rigid)";
							// Assign the game object to the Unity object matching the subject name.
							o = objectDict[subject.ToLower()];
						}
					}
					else // If it is not one of the resetable rigid bodies, then it must be part of a skeleton.
					{
						// Body joints match on segment name.  If we can't find an object in the
						// Unity scene graph that matches the segment name, continue onto the next object.
						if ( !objectDict.ContainsKey( item.name.ToLower() ) )
						{
							infoMessage += "\n  body missing:" + item.name.ToLower ();
							continue;
						}
						infoMessage += "\n   body: " + item.name.ToLower();
						// Assign the game object to the Unity object matching the segment name.
						o = objectDict[item.name.ToLower()];
					}

					// If we didn't find a matching Unity object, continue onto the next segment
					if( o == null) continue;

					// As per the description at the top of this file,
					// This is the logic which triggers the resetting of the orientation of resetable rigid bodies.
					bool doHands  = isHands && captureOriHands;
					bool doFeet   = isFeet && captureOriFeet;
					bool doBody   = (!isOculus && !isHands && !isFeet && captureOriBody);
					bool doOculus = isOculus && captureOriOculus;

					// If we are going to reset one of the resetable rigid bodies...
					if (  doHands || doFeet || doBody || doOculus ) 
					{
						// Create or update the rotation offset associated with this Unity object to be right (1, 0, 0) unit vector rotated
						// by the inverse orientation of the current rigid bodies orientation.
						rotationOffsets[o] =  Quaternion.AngleAxis(0, Vector3.right) * Quaternion.Inverse ( localOrientation );
					}

					// Set object visibility
					o.SetActive(true);

					// Set Translation and rotation
					//o.transform.localPosition = new Vector3(-item.tr[0] / 100, -item.tr[2] / 100, item.tr[1] / 100);
					o.transform.localPosition = new Vector3(item.tr[0]*worldScale, item.tr[1]*worldScale, item.tr[2]*worldScale);
					o.transform.localRotation = localOrientation * rotationOffsets[o];

					if( VRoamTransform ) {
						// Update camera position (first add Offset to parent transform)
						Vector3 p = Quaternion.Inverse(VRoamTransform.rotation) * o.transform.position;
						p.Scale(VRoamTransform.localScale);
						p += VRoamTransform.position;
						o.transform.position = p;

						// Multiply the camera controller's rotation by the InteractionCompass rotation
						Quaternion q = VRoamTransform.rotation * o.transform.rotation;
						o.transform.rotation = q;
					}

					frameDone = true;
				}
				else
				{
					if(showMarkers)
					{
						// Marker

						//Vector3 pos = new Vector3(-item.tr[0] / 100, -item.tr[2] / 100, item.tr[1] / 100);
						Vector3 pos = new Vector3(item.tr[0], item.tr[1], item.tr[2]);

						if(markerDict.ContainsKey(item.name))
						{
							markerDict[item.name].transform.localPosition = pos;
						}
						else
						{
							GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
							cube.transform.parent = markerGroup.transform;
							cube.name = "marker_" + item.name;
							cube.transform.localScale = new Vector3(markerSize, markerSize, markerSize);
							markerDict.Add (item.name, cube);
							cube.transform.localPosition = pos;
						}
						frameDone = true;
					}
				}
			}

			infoMessage += "\n";

			if(frameDone) frameCount++;
		}

		captureOriOculus = false;
		captureOriHands = false;
		captureOriFeet = false;
		captureOriBody = false;
	}
}
