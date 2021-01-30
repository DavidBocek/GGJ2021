using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

public class PlayerController : MonoBehaviour, ICharacterController
{
	[Header("Movement")]
	public float walkSpeed;
	public float walkAcceleration;
	public float runSpeed;
	public float runAcceleration;
	public float stopDeceleration;
	public float fallSpeedMax;
	public float gravityAccel;
	public float runTimeMax;
	public float runTimeRechargeMultiplier;
	public float runCooldownTime;
	private bool m_isRunning;
	private bool m_inRunCooldown;
	private float m_runTime;
	public float minLookXAngle;
	public float maxLookXAngle;
	public float horizontalSensitivity;
	public float verticalSensitivity;
	private Quaternion m_cameraRotation;
	private Quaternion m_characterHorizontalRotation;

	[Header("Headbob")]
	public float headbobAmplitudeMax;
	public float headbobFrequencyWalk;
	public float headbobFrequencyRun;
	public float headbobSpeedMin;
	public float headbobSpeedMax;
	private bool m_isHeadbobbing;
	private Vector3 m_initialCamLocalPos;
	private float m_curHeadbob;
	private float m_curHeadbobFrequency;
	private float m_headbobStartTime;



	//refs
	private GameObject m_cameraObj;
	private KinematicCharacterMotor m_motor;

    void Start()
    {
		m_cameraObj = GetComponentInChildren<Camera>().gameObject;
		m_motor = GetComponent<KinematicCharacterMotor>();
		m_motor.CharacterController = this;

		m_isRunning = false;
		m_runTime = runTimeMax;
		m_inRunCooldown = false;

		m_curHeadbob = 0f;
		m_curHeadbobFrequency = headbobFrequencyWalk;

		m_initialCamLocalPos = m_cameraObj.transform.localPosition;
		m_cameraRotation = m_cameraObj.transform.localRotation;
		m_characterHorizontalRotation = transform.rotation;
    }

    void Update()
    {
		//look
		Vector2 input = new Vector2( Input.GetAxis( "Mouse X" ), Input.GetAxis( "Mouse Y" ) );
		m_cameraRotation *= Quaternion.Euler( -input.y, 0f, 0f );
		m_cameraRotation = ScriptUtil.ClampRotationAroundXAxis( m_cameraRotation, minLookXAngle, maxLookXAngle );
		m_characterHorizontalRotation = m_characterHorizontalRotation * Quaternion.Euler( 0f, input.x, 0f );
		m_cameraObj.transform.localRotation = m_cameraRotation;

		//headbob
		Vector3 cameraOffset = m_initialCamLocalPos;
		cameraOffset.y += m_curHeadbob;
		m_cameraObj.transform.localPosition = cameraOffset;
    }

	public void UpdateRotation( ref Quaternion currentRotation, float deltaTime )
	{
		currentRotation = m_characterHorizontalRotation;
	}

	public void UpdateVelocity( ref Vector3 currentVelocity, float deltaTime )
	{
		Vector2 input = new Vector2( Input.GetAxisRaw( "Horizontal" ), Input.GetAxisRaw( "Vertical" ) );
		if ( Input.GetButton( "Sprint" ) && !m_inRunCooldown && m_runTime >= 0f )
			m_isRunning = true;
		else
			m_isRunning = false;

		//get desired vel
		Vector3 desiredVel = new Vector3( input.x, 0f, input.y );
		desiredVel = transform.TransformDirection( desiredVel );
		desiredVel.Normalize();

		Vector3 desiredVelRight = Vector3.Cross( desiredVel, m_motor.CharacterUp );
		Vector3 slopeOrientedVel = Vector3.Cross( m_motor.GroundingStatus.GroundNormal, desiredVelRight ).normalized * desiredVel.magnitude;
		float moveSpeed = m_isRunning ? runSpeed : walkSpeed;
		desiredVel = slopeOrientedVel * moveSpeed;

		//add vel if we are inputting
		Vector3 accelVec = Vector3.zero;
		float accel = 0f;
		if ( Mathf.Abs( input.x ) > 0f || Mathf.Abs( input.y ) > 0f )
		{
			accelVec = desiredVel - currentVelocity;
			accelVec.Normalize();
			accel = m_isRunning ? runAcceleration : walkAcceleration;
		}
		//else deccel towards 0
		else
		{
			accelVec = -currentVelocity;
			accelVec.Normalize();
			accel = stopDeceleration;
			desiredVel = Vector3.zero;
		}	
		accelVec *= accel * deltaTime;
		accelVec = Vector3.ClampMagnitude( accelVec, (desiredVel - currentVelocity).magnitude );
		currentVelocity += accelVec;

		//update run vals (after movement stuff )
		if ( m_isRunning )
		{
			m_runTime -= deltaTime;
			if ( m_runTime <= 0f )
			{
				m_runTime = 0f;
				m_isRunning = false;
				m_inRunCooldown = true;
			}
		}
		else
		{
			m_runTime = Mathf.Min( m_runTime + deltaTime * runTimeRechargeMultiplier, runTimeMax );
			if ( m_inRunCooldown && m_runTime >= runCooldownTime )
				m_inRunCooldown = false;
		}

		//gravity
		if ( !m_motor.GroundingStatus.IsStableOnGround )
		{
			currentVelocity += Vector3.down * gravityAccel * deltaTime;
			currentVelocity.y = Mathf.Max( -fallSpeedMax, currentVelocity.y );
		}

		//headbob
		float speed = m_motor.BaseVelocity.magnitude;
		float curHeadbobAmplitude = 0f;
		if ( speed >= 0.2f )
		{
			if ( !m_isHeadbobbing )
			{
				m_isHeadbobbing = true;
				m_headbobStartTime = Time.time;
			}

			curHeadbobAmplitude = MathUtil.RemapClamped( speed, headbobSpeedMin, headbobSpeedMax, 0f, headbobAmplitudeMax );
		}
		else
		{
			m_isHeadbobbing = false;
			m_curHeadbob = Mathf.Lerp( m_curHeadbob, 0f, ScriptUtil.GetAsymptoticLerpFrac( 0.3f, deltaTime ) );
		}


		if ( m_isHeadbobbing )
		{
			float desiredHeadbobFreq = m_isRunning ? headbobFrequencyRun : headbobFrequencyWalk;
			m_curHeadbobFrequency = ScriptUtil.AsymptoticLerp( m_curHeadbobFrequency, desiredHeadbobFreq, 0.3f, deltaTime );
			m_curHeadbob = curHeadbobAmplitude * Mathf.Sin( m_curHeadbobFrequency * (Time.time - m_headbobStartTime) );
		}
		else
		{
			m_curHeadbobFrequency = ScriptUtil.AsymptoticLerp( m_curHeadbobFrequency, headbobFrequencyWalk, 0.3f, deltaTime );
		}
	}


	public void BeforeCharacterUpdate( float deltaTime )
	{
		return;
	}

	public void PostGroundingUpdate( float deltaTime )
	{
		return;
	}

	public void AfterCharacterUpdate( float deltaTime )
	{
		return;
	}

	public bool IsColliderValidForCollisions( Collider coll )
	{
		return true;
	}

	public void OnGroundHit( Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport )
	{
		return;
	}

	public void OnMovementHit( Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport )
	{
		return;
	}

	public void ProcessHitStabilityReport( Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport )
	{
		return;
	}

	public void OnDiscreteCollisionDetected( Collider hitCollider )
	{
		return;
	}
}
