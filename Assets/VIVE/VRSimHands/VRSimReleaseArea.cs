using UnityEngine;
//using System.Collections;
using System.Collections.Generic;
using System;
using Holoville.HOTween;


/// <summary>
/// Receive Areas require a specific set of objects be given to them.
///  When a VRSimHandInteraction object passes into the receiveArea and releasses an object,
///  The box is hilighted either Green or Red
///  If correct object given, the box flashes green, a "correct" sound is played, and the object
///  is held there until all expected tools are received
///  If an incorrect object is given, the box flashes red, a "incorrect" sound is played, and the
///  object is immediately returned home.
///  ** Once all tools are received, then all objects are returned home.
///    (to be changed in the future to ingest some materials)
/// </summary>

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class VRSimReleaseArea : MonoBehaviour {
	List<VRSimHandInteraction> m_hands = new List<VRSimHandInteraction>();
	List<VRSimPickupObject> m_receivedObjects = new List<VRSimPickupObject>();

    public Color NormalColor;
    public List<string> AcceptList = new List<string>();
	
	public AudioClip AskClip;

	public AudioClip AcceptSound;
	public Color AcceptColor;
	
	public AudioClip RejectSound;
	public Color RejectColor;

    public void setAcceptList(List<string> list)
    {
        resetReceived();

        m_receivedObjects.Clear();
        AcceptList.Clear();

        if (list != null)
        {
            // ReceivedObjects uses a padding of "null" references to do quick checks on accepted strings.
            for (int i = 0; i < list.Count; i++)
            {
                m_receivedObjects.Add(null);
            }

            AcceptList.AddRange(list);
        }
    }

    // Clear all the colour, state, and receivedObjects.
    void resetReceived()
    {
        for (int i = 0; i < m_receivedObjects.Count; i++)
        {
            VRSimPickupObject o = m_receivedObjects[i];
            if (o != null)
            {
                o.returnHome();
                m_receivedObjects[i] = null;
            }
        }
    }

	// Use this for initialization
	void Start () {
		// ReceivedObjects uses a padding of "null" references to do quick checks on accepted strings.
		m_receivedObjects = new List<VRSimPickupObject>();
		for( int i=0; i<AcceptList.Count; i++ ) {
			m_receivedObjects.Add(null);
		}
	}

	public bool receiveObject( VRSimPickupObject pickupObj ) {
		for( int i=0; i<AcceptList.Count; i++ ) {
			if( m_receivedObjects[i] == null
			   && AcceptList[i].Equals(pickupObj.gameObject.name))
            {
                m_receivedObjects[i] = pickupObj;
                pickupObj.transform.parent = transform;

                acceptAction( pickupObj );
				return true;
			}
		}

        pickupObj.returnHome();
        rejectAction();
        return false;
	}
	
    public bool receivedAllObjects() {
        foreach( VRSimPickupObject o in m_receivedObjects ) {
            if( o == null ) return false;
        }
        
        return true;
    }
    
	// Check through all pickupObjects that we are the parent of,
	// hilight the nearest if a Hand Interaction is in the volume.
	void Update() {
	}
    
    void strobeColour(Color toColor, Color returnColor, float duration) {
        TweenParms toParms = new TweenParms().Prop("color", toColor).Ease(EaseType.EaseOutExpo);
        TweenParms returnParms = new TweenParms().Prop("color", returnColor).Ease(EaseType.EaseInOutExpo);
        Sequence seq = new Sequence();
        seq.Append(HOTween.To(renderer.material, duration/2, toParms));
        seq.Append(HOTween.To(renderer.material, duration/2, returnParms));
        seq.Play();
    }

    void acceptAction(VRSimPickupObject pickupObj) {
        strobeColour(AcceptColor, NormalColor, 0.5f);
        if (AcceptSound) {
            AudioSource.PlayClipAtPoint(AcceptSound, transform.position);
        }
    }

    void rejectAction()
    {
        strobeColour(RejectColor, NormalColor, 0.5f);
        if (RejectSound)
        {
            AudioSource.PlayClipAtPoint(RejectSound, transform.position);
        }
    }

	
	void OnTriggerEnter(Collider other) {
		VRSimHandInteraction hand = (VRSimHandInteraction)other.GetComponent(typeof(VRSimHandInteraction));
		if( hand != null ) {
            hand.didEnterReleaseArea(this);
			m_hands.Add(hand);
		}
	}

	void OnTriggerExit(Collider other) {
		VRSimHandInteraction hand = (VRSimHandInteraction)other.GetComponent(typeof(VRSimHandInteraction));
		if( hand != null ) {
			hand.didLeaveReleaseArea( this );
			m_hands.Remove(hand);
		}
	}
}
