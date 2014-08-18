using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SphereCollider))]

public class VRSimHandInteraction : MonoBehaviour {
	protected Animator			m_animator = null;
	protected VRSimPickupArea   m_pickupArea = null;
    protected VRSimReleaseArea  m_releaseArea = null;
    protected VRSimPickupObject m_pickupObject = null;
	
	public VRSimPickupObject  PickupObject
	{
		get{return m_pickupObject;}
		protected set{m_pickupObject = value;}
	}

	public Vector3  PickupPoint
	{
		get{
			SphereCollider sphereCollider = (SphereCollider)collider;
			return transform.TransformPoint(sphereCollider.center);
		}
	}
	
	// Use this for initialization
	void Start () {
		m_animator = this.GetComponent<Animator>();	
	}
	
	/// <summary>
	/// Pickup an object or drop it, depending on state.
	/// </summary>
	void Update () {
		bool fist = m_animator.GetBool("Fist");
		if( fist ) {
            // Pickup object
			if( m_pickupObject == null && m_pickupArea != null )
			{ 
				this.pickupObject();
			}
		} else {
			if( m_pickupObject != null )
			{ // Drop object
                if (m_releaseArea != null)
                {
                    m_pickupObject.ParentHand = null;
                    m_releaseArea.receiveObject(m_pickupObject);
                }
                else
                {
                    m_pickupObject.returnHome();
                }

				PickupObject = null;
			}
		}
	}

	//---- Pickup Mechanics -----
	
	public void didEnterPointingArea( VRSimPickupArea area ) {
		m_animator.SetBool("Point", true);
		m_pickupArea = area;
	}

	public void didLeavePointingArea( VRSimPickupArea area ) {
		m_animator.SetBool("Point", false);
		m_pickupArea = null;
	}

	protected void pickupObject() {
		if(m_pickupArea == null) return;
		
        VRSimPickupObject obj = m_pickupArea.pickupObject(this);
		if( obj != null ) {
            PickupObject = obj;
            m_pickupObject.ParentHand = this;
        }
        else
        {
            // Nothing to pickup, so attempt picking up from the other hand.

        }
	}

	//---- Receive Mechanics -----
	public void didEnterReleaseArea( VRSimReleaseArea area ) {
		m_releaseArea = area;
	}

    public void didLeaveReleaseArea( VRSimReleaseArea area)
    {
    	m_releaseArea = null;
	}
}
