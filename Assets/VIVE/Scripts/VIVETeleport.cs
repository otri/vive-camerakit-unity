// VIVETeleport
// Transportation between compass markers, so we can jump around the world quickly.
//
//  Use the paired PSMove controllers:
//   Move button - select compass and jump to it

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(VIVECameraController))]
public class VIVETeleport : MonoBehaviour {
	public bool teleport() {
		VIVECameraController vivecontroller = GetComponent<VIVECameraController>();
		// Look for nearest compass, and jump there.
		GameObject nearestCompass = findNearestLineOfSightCompass();
		
		// Go there.
		if( nearestCompass ) {
			vivecontroller.VRoamCompass = nearestCompass.transform;
			return true;
		} else {
			return false;
		}
	}

	GameObject findNearestLineOfSightCompass() {

		// Ignore the current interactionCompass
		VIVECameraController vivecontroller = GetComponent<VIVECameraController>();
		Transform interactionCompass = vivecontroller.VRoamCompass;

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

		// Pick the nearest to forward/center match.
		visibleCompasses.Sort((objectA, objectB) => {
			Vector3 vectorToA = objectA.transform.position - camTransform.position;
			Vector3 vectorToB = objectB.transform.position - camTransform.position;

			// Check if we're poiting at it.
			float angleToA = Vector3.Angle(camForward, vectorToA);
			float angleToB = Vector3.Angle(camForward, vectorToB);

			return angleToA.CompareTo(angleToB);
		});

		GameObject target = visibleCompasses[0];
		{
			Vector3 dirToCompass = target.transform.position - camTransform.position;
			float angle = Vector3.Angle(camForward, dirToCompass);
			Debug.Log("Jumping to "+target.name+", angle("+angle+") distance("+dirToCompass.magnitude+")");
		}

		return target;
	}
}
