// VIVETeleport
// Transportation between compass markers, so we can jump around the world quickly.
//
//  Use the paired PSMove controllers:
//   Move button - select compass and jump to it

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
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(VIVECameraController))]
public class VIVETeleportController : MonoBehaviour {
	Stack<GameObject> history = new Stack<GameObject>();

	// PAGE UP and PAGE DOWN are teleport commands
	void OnGUI()
	{
		Event e = Event.current;
		if (e.type == EventType.KeyUp)
		{
			if(e.keyCode == KeyCode.PageDown) {
				Debug.Log("Teleport from Presenter PgDown");
				teleport();
			} else if( e.keyCode == KeyCode.PageUp ) {
				Debug.Log("UnTeleport from Presenter PgUp");
				unTeleport();
			}
		} 
	}

	public bool teleport() {
		VIVECameraController vivecontroller = GetComponent<VIVECameraController>();
		// Look for nearest compass, and jump there.
		GameObject nearestCompass = findNearestLineOfSightCompass();
		
		// Go there.
		if( nearestCompass ) {
			// Push the current interactionCompass on the history.
			GameObject interactionCompass = vivecontroller.VRoamCompass;
			history.Push(interactionCompass);

			vivecontroller.VRoamCompass = nearestCompass;
			return true;
		} else {
			return false;
		}
	}

	public bool unTeleport() {
		if( history.Count > 0 ) {
			GameObject interactionCompass = history.Pop();
			VIVECameraController vivecontroller = GetComponent<VIVECameraController>();
			vivecontroller.VRoamCompass = interactionCompass;
			return true;
		} else {
			return false;
		}
	}

	public void Update() {
		GameObject[] compasses = GameObject.FindGameObjectsWithTag("VIVECompass");
		foreach( GameObject compass in compasses ) {
			compass.renderer.material.color = Color.white;
		}

		GameObject o = findNearestLineOfSightCompass();
		if( o ) {
			o.renderer.material.color = Color.blue;
		}
	}

	GameObject findNearestLineOfSightCompass() {

		// Ignore the current interactionCompass
		VIVECameraController vivecontroller = GetComponent<VIVECameraController>();
		GameObject interactionCompass = vivecontroller.VRoamCompass;

		// If we're on an interactionCompass, then we have to transform our coordinate system to match.
		Transform camTransform = vivecontroller.GetCameraTransform();
		Vector3 camForward = camTransform.forward;

		GameObject[] compasses = GameObject.FindGameObjectsWithTag("VIVECompass");
		List<GameObject> visibleCompasses = new List<GameObject>();

		foreach( GameObject compass in compasses )
		{
			if(compass.transform == interactionCompass ) // Ignore interactionCompass, we're already on it.
				continue;

			Vector3 dirToCompass = (compass.transform.position - camTransform.position);
			float distance = dirToCompass.magnitude;
			if( distance < 0.05f )
			{
				// We're standing right on it.
				visibleCompasses.Add(compass);
			} else
			{
				// Check if we're poiting at it.
				float angle = Vector3.Angle(camForward, dirToCompass);
				if( angle < 20 )  // Are we in the 20 degree cone from our forward orientation?
				{
					visibleCompasses.Add(compass);
				}
			}
		}

		if( visibleCompasses.Count == 0 ) {
			return null;
		}

		// sort for the nearest.
		visibleCompasses.Sort((objectA, objectB) => {
			float distA = Vector3.Distance(camTransform.position, objectA.transform.position);
			float distB = Vector3.Distance(camTransform.position, objectB.transform.position);
			float relDist = distB - distA;
			if( relDist > 0 ) return -1;
			else if( relDist < 0 ) return 1;
			return 0;
		});

		GameObject target = visibleCompasses[0];
		Vector3 dirTo = target.transform.position - camTransform.position;
		float msgAngle = Vector3.Angle(camForward, dirTo);
		Debug.Log("Jumping to "+target.name+", angle("+msgAngle+") distance("+dirTo.magnitude+")");

		return target;
	}
}
