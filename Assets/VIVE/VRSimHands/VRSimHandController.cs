
//
// Copyright (C) 2013 Sixense Entertainment Inc.
// All Rights Reserved
//

using UnityEngine;
using System.Collections;

/// <summary>
/// Hand controller is bound to.
/// </summary>
public enum VRSimHands
{
	UNKNOWN = 0,
	LEFT = 1,
	RIGHT = 2,
}

[RequireComponent(typeof(Animator))]
public class VRSimHandController : MonoBehaviour
{
	public float				fistPull=0;
	public VRSimHands			Hand;
	
	protected virtual void Start() 
	{
	}

	protected virtual void Update()
	{
		Animator animator = GetComponent<Animator>();

		// Update fist tracking.
		animator.SetFloat("FistAmount", fistPull);
		animator.SetBool( "Fist", fistPull > 0.9f );
	}
}

