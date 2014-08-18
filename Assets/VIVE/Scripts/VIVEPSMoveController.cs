// VIVEPSMoveController
// Playstation Move API controller interface for manipulating objects with hands in VR.

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
public class VIVEPSMoveController : MonoBehaviour
{
	private PSMoveButton teleportButton = PSMoveButton.Move;
	private bool teleportActivated = false;

	public VRSimHandController rightHandController = null;
//	private AHRS.MahonyAHRS rightSensorFusion = null;
	public VRSimHandController leftHandController = null;

	private List<UniMoveController> moves = new List<UniMoveController>();
	private System.Threading.Timer buzzTimer;

	// Use this for initialization
	void Start ()
	{
		// Keep the updates coming at 10Hz
		Time.maximumDeltaTime = 0.1f;
		int count = UniMoveController.GetNumConnected();

		// Iterate through all connections (USB and Bluetooth)
		for (int i = 0; i < count; i++) 
		{
			UniMoveController move = gameObject.AddComponent<UniMoveController>();	// It's a MonoBehaviour, so we can't just call a constructor
			
			// Remember to initialize!
			if (!move.Init(i)) 
			{	
				Destroy(move);	// If it failed to initialize, destroy and continue on
				continue;
			}
			
			// This example program only uses Bluetooth-connected controllers
			PSMoveConnectionType conn = move.ConnectionType;
			if (conn == PSMoveConnectionType.Unknown || conn == PSMoveConnectionType.USB) 
			{
				Destroy(move);
			}
			else 
			{
				moves.Add(move);
				
				move.OnControllerDisconnected += HandleControllerDisconnected;
				
				// Start all controllers with a white LED
				move.SetLED(Color.grey);
			}
		}

//		rightSensorFusion = new AHRS.MahonyAHRS(Time.deltaTime); // Assume 60Hz update rate
	}
	
	// Update is called once per frame
	void Update()
	{
		UpdateTeleportTrigger();
		UpdateHands();
	}

	void UpdateTeleportTrigger() {
		// Update Teleport trigger
		VIVETeleport viveteleport = GetComponent<VIVETeleport>();
		foreach(UniMoveController move in moves)
		{
			// Instead of this somewhat kludge-y check, we'd probably want to remove/destroy
			// the now-defunct controller in the disconnected event handler below.
			if(move.Disconnected) continue;
			
			// Detect move action
			if( viveteleport != null && move.GetButtonDown(teleportButton) ) {
				// Activate jumping, ONCE.
				if( !teleportActivated ) {
					teleportActivated = true;
					
					if( viveteleport.teleport() ) {
						buzzMove(move, 1, Color.green, 0.25);
					} else {
						buzzMove(move, 0.5f, Color.red, 0.10);
					}
				}
			} else {
				teleportActivated = false;
			}
		}
	}

	void UpdateHands() {
		// FIXME: Just update first hand we match.
		if( rightHandController == null ) return;
		if( moves.Count < 1 ) return;
		UniMoveController rightMove = moves[0];

		// Fist pulling gesture.
		rightHandController.fistPull = rightMove.Trigger;

		// Update sensor fusion computation of the rigid body xform.
//		Vector3 gyro = rightMove.Gyro;
//		Vector3 accel = rightMove.Acceleration;
//		rightSensorFusion.SamplePeriod = Time.deltaTime;
//		rightSensorFusion.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z);
//		float[] rightQuat = rightSensorFusion.Quaternion;
//		rightHandController.transform.rotation = new Quaternion(rightQuat[0], rightQuat[1], rightQuat[2], rightQuat[3]);
	}

	void HandleControllerDisconnected(object sender, EventArgs e)
	{
		// TODO: Remove this disconnected controller from the list and maybe give an update to the player
		Debug.Log("PS Move controller disconnected." );
	}

	void buzzMove(UniMoveController move, float amount, Color color, double duration ) {
		// Play a rumble at a given level and duration.
		move.SetRumble(amount);
		move.SetLED(color);
		if(buzzTimer != null ) {
			buzzTimer.Dispose();
			buzzTimer = null;
		}
		
		buzzTimer = new System.Threading.Timer(
			obj => {
			move.SetLED(Color.grey);
			move.SetRumble(0);
			buzzTimer = null;
		},
		null,
		(uint)(duration*1000),
		System.Threading.Timeout.Infinite);
	}
}

