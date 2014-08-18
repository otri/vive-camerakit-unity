using UnityEngine;
//using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Pickup Areas keep a collection of objects that can be picked up.
///  When a VRSimHandInteraction object, not carrying a pickup object, passes into the pickupArea,
///  the interaction is notified, and the nearest pickup object is hilighted.
///  When the Interaction object goes to "pickup" the object the nearest is handed over.
/// </summary>


[RequireComponent(typeof(Collider))]
public class VRSimPickupArea : MonoBehaviour {
	public float PickDistance;

	List<VRSimHandInteraction> m_hands = new List<VRSimHandInteraction>();
	
	// Use this for initialization
	void Start () {
		
	}
	
	protected VRSimPickupObject NearestObjectToHand( VRSimHandInteraction hand ) {
		Component[] pickupObjects = gameObject.GetComponentsInChildren<VRSimPickupObject>();

		VRSimPickupObject nearest = null;
		float nearestMagnitude = float.MaxValue;
		float pickDistSquared = PickDistance*PickDistance;
		foreach( VRSimPickupObject obj in pickupObjects ) {
			float magnitude = ( hand.PickupPoint - obj.transform.position ).sqrMagnitude;
			if( magnitude < nearestMagnitude && magnitude < pickDistSquared ) {
				nearest = obj;
				nearestMagnitude = magnitude;
			}
		}
		
		return nearest;
	}
	
	// Check through all pickupObjects that we are the parent of,
	// hilight the nearest if a Hand Interaction is in the volume.
	void Update() {
		Component[] pickupObjects = gameObject.GetComponentsInChildren<VRSimPickupObject>();
		if( pickupObjects == null ) return;
		
		List<VRSimPickupObject> nearestPickupObjects = new List<VRSimPickupObject>(m_hands.Count);
		foreach( VRSimHandInteraction hand in m_hands ) {

			// Skip hands with something in them.
			if( hand.PickupObject != null ) continue;
				
			VRSimPickupObject nearest = NearestObjectToHand(hand);
			if(nearest) {
			 	nearestPickupObjects.Add(nearest);
			}
		}
		
		foreach( VRSimPickupObject obj in pickupObjects ) {
			if( obj.Hilight == true && nearestPickupObjects.Contains(obj)==false ) {
				obj.Hilight = false;
			} else if( obj.Hilight == false && nearestPickupObjects.Contains(obj) ) {
				obj.Hilight = true;
			}
		}
	}
	
	void OnTriggerEnter(Collider other) {
		VRSimHandInteraction hand = (VRSimHandInteraction)other.GetComponent(typeof(VRSimHandInteraction));
		if( hand != null ) {
			hand.didEnterPointingArea( this );
			m_hands.Add(hand);
		}
	}

	void OnTriggerExit(Collider other) {
		VRSimHandInteraction hand = (VRSimHandInteraction)other.GetComponent(typeof(VRSimHandInteraction));
		if( hand != null ) {
			hand.didLeavePointingArea( this );
			m_hands.Remove(hand);
		}
	}
	
	/// <summary>
	///   Attempts to pickup an object of type VRSimPickupObject
	/// </summary>
	/// <returns>The object.</returns>
	public VRSimPickupObject pickupObject( VRSimHandInteraction hand ) {
		VRSimPickupObject nearestObject = NearestObjectToHand(hand);
		if( nearestObject ) {
			nearestObject.Hilight = false;
		}
		return nearestObject;
	}
}
