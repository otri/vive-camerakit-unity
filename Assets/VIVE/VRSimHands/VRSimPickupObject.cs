using UnityEngine;
using System.Collections;
using Holoville.HOTween;

[RequireComponent(typeof(AudioSource))]
public class VRSimPickupObject : MonoBehaviour
{
    protected Color m_hilightColor;
    protected GameObject m_hilightGameObject;
	protected Vector3 m_homeWorldPosition;
	protected Quaternion m_homeWorldRotation;
	protected Vector3 m_homeLocalScale;
	protected Transform m_homeParent;
	protected bool m_isPickedUp;
	protected bool m_hilight;
    protected AudioClip m_onHilightSound;
    protected AudioClip m_offHilightSound;
    protected VRSimHandInteraction m_parentHand;

    public VRSimHandInteraction ParentHand
    {
        get{return m_parentHand;}
        set{
            m_isPickedUp = true;
            m_parentHand = value;
            if (m_parentHand != null)
            {
                transform.parent = null;
            }
        }
    }
	
	public bool Hilight
	{
		get{return m_hilight;}
		set{
            if (m_hilight != value) {
                m_hilight = value;
                
                if( m_hilight )
                {
                    m_hilightGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Bounds myBounds = GetComponent<Collider>().bounds;
                    m_hilightGameObject.transform.localPosition = myBounds.center;
                    m_hilightGameObject.transform.localScale = myBounds.size * 1.1f;
                    m_hilightGameObject.transform.parent = transform;
                    m_hilightGameObject.renderer.material = new Material(Shader.Find("Transparent/Diffuse"));
                    m_hilightGameObject.renderer.material.color = Color.clear;
                    TweenParms parms = new TweenParms();
                    parms.Prop("color", m_hilightColor );
                    parms.Ease(EaseType.EaseInOutExpo);
                    HOTween.To(m_hilightGameObject.renderer.material, 0.25f, parms);
                    
                    if (m_onHilightSound)
                    {
                        AudioSource.PlayClipAtPoint(m_onHilightSound, transform.position, 0.2f);
                    }
                } else {
                    TweenParms parms = new TweenParms();
                    parms.Prop("color", Color.clear );
                    parms.Ease(EaseType.EaseInOutExpo);
                    GameObject hilightToBeRemoved = m_hilightGameObject; // Capture the current HilightGameObject for lambda to Destroy.
                    parms.OnComplete( () => Destroy(hilightToBeRemoved) );
                    HOTween.To(m_hilightGameObject.renderer.material, 0.25f, parms);
                    
                    if (m_offHilightSound)
                    {
                        AudioSource.PlayClipAtPoint(m_offHilightSound, transform.position, 0.2f);
                    }
                }
            }
		}
	}
	
	// Use this for initialization
	void Start()
	{
        m_onHilightSound = Resources.Load<AudioClip>("sounds/vrsim_object_on_hover");
        m_offHilightSound = Resources.Load<AudioClip>("sounds/vrsim_object_off_hover");
        
        m_hilightColor = new Color(0.98f,1.0f,0.0f,0.5f);
		m_homeParent = transform.parent;
		m_homeWorldPosition = transform.position;
		m_homeWorldRotation = transform.rotation;
		m_homeLocalScale = transform.localScale;
	}

	// Update is called once per frame
	void Update ()
	{
        if( m_parentHand ) {
            transform.position = m_parentHand.PickupPoint;
            transform.rotation = m_parentHand.transform.rotation;
        }
	}
	
	public void returnHome() {
        ParentHand = null;
		TweenParms parms = new TweenParms();
		parms.Prop("position", m_homeWorldPosition );
		parms.Prop("rotation", m_homeWorldRotation );
		parms.Ease(EaseType.EaseInOutExpo);
		parms.Delay(0.25f);
		parms.OnComplete(returnHomeCompleted);
		HOTween.To(transform, 0.5f, parms);
    }
	
	private void returnHomeCompleted() {
        transform.parent = m_homeParent;
        transform.position = m_homeWorldPosition;
        transform.rotation = m_homeWorldRotation;
        transform.localScale = m_homeLocalScale;
	}
}

