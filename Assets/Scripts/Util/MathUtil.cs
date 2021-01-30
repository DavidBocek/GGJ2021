using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtil
{
	// =====================================================================================
	//
	// SHARED CONSTS
	//
	// =====================================================================================
	public const float SQRT2OVER2 = 0.70710678118f;

	// =====================================================================================
	//
	// VALUE REMAPPING, CLAMPING, AND INTERPOLATING
	//
	// =====================================================================================
	public static float Remap(float val, float from1, float to1, float from2, float to2)
	{
		return (val - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	public static float RemapFrom01(float val, float from2, float to2)
	{
		return from2 + val * (to2 - from2);
	}

	public static float RemapTo01(float val, float from1, float to1)
	{
		return (val - from1) / (to1 - from1);
	}

	public static float RemapClamped(float val, float from1, float to1, float from2, float to2)
	{
		return MathUtil.Remap( Mathf.Clamp( val, Mathf.Min( from1, to1 ), Mathf.Max( from1, to1 ) ), from1, to1, from2, to2 );
	}

	public static float RemapTo01Clamped( float val, float from1, float to1 )
	{
		return Mathf.Clamp01( RemapTo01( val, from1, to1 ) );
	}

	public static float LerpUnclamped(float from, float to, float tUnclamped)
	{
		return from + (to - from) * tUnclamped;
	}

	public static void ClampVectorExtents( ref Vector2 vec, Vector2 maxMagnitudes )
	{
		vec.x = Mathf.Clamp( vec.x, -maxMagnitudes.x, maxMagnitudes.x );
		vec.y = Mathf.Clamp( vec.y, -maxMagnitudes.y, maxMagnitudes.y );
	}

	public static void ClampVectorExtents( ref Vector3 vec, Vector3 maxMagnitudes )
	{
		vec.x = Mathf.Clamp( vec.x, -maxMagnitudes.x, maxMagnitudes.x );
		vec.y = Mathf.Clamp( vec.y, -maxMagnitudes.y, maxMagnitudes.y );
		vec.z = Mathf.Clamp( vec.z, -maxMagnitudes.z, maxMagnitudes.z );
	}

	public static void EpsilonZeroOutVectorByParts( ref Vector3 vec, float epsilon )
	{
		if ( Mathf.Abs( vec.x ) <= epsilon )
			vec.x = 0f;
		if ( Mathf.Abs( vec.y ) <= epsilon )
			vec.y = 0f;
		if ( Mathf.Abs( vec.z ) <= epsilon )
			vec.z = 0f;
	}

	// =====================================================================================
	//
	// RANDOM
	//
	// =====================================================================================
	public static bool RandomBool()
	{
		return Random.value > 0.5f;
	}
		
	//distribution power must be > 0. 1 is uniform, lower weights towards max, higher weights towards min. if random value override is provided, it should be in range [0,1]
	public static float RandomRangeExponentialWeighted(float min, float max, float distributionPower, float randomValueOverride = -1)
	{
		float randVal = randomValueOverride > 0f ? randomValueOverride : Random.value;
		return min + (max - min) * Mathf.Pow(randVal, distributionPower);
	}

	//Uses Box-Muller transform: https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform
	//returns 2 approximated gaussian values with supplied mean and stdDev
	public static Vector2 RandomRangeGaussian( float stdDev, float mean = 0f )
	{
		float u;
		float v;
		float s;
		do
		{
			u = Random.Range( -1f, 1f );
			v = Random.Range( -1f, 1f );
			s = u * u + v * v;
		} while ( s <= Mathf.Epsilon || s >= 1f );

		float z0 = u * Mathf.Sqrt( (-2f * Mathf.Log( s )) / s );
		float z1 = v * Mathf.Sqrt( (-2f * Mathf.Log( s )) / s );

		return new Vector2( z0 * stdDev + mean, z0 * stdDev + mean );
	}
	
	//similar to above, using Box-Muller, but only calculates one rand
	public static float RandomRangeGaussianSingle( float stdDev, float mean = 0f )
	{
		float u;
		float v;
		float s;
		do
		{
			u = Random.Range( -1f, 1f );
			v = Random.Range( -1f, 1f );
			s = u * u + v * v;
		} while ( s <= Mathf.Epsilon || s >= 1f );

		float z0 = u * Mathf.Sqrt( (-2f * Mathf.Log( s )) / s );

		return z0 * stdDev + mean;
	}

	public static int RandomWeightedIdx(List<int> weights)
	{
		int cumulativeWeight = 0;
		foreach (int weight in weights)
		{
			cumulativeWeight += weight;
		}

		int rand = Random.Range(0, cumulativeWeight);
		int weightSoFar = 0;
		for (int i=0; i<weights.Count; i++)
		{
			int weight = weights[i];
			weightSoFar += weight;
			if (rand < weightSoFar)
			{
				return i;
			}
		}

		throw new UnityException("RandomWeightedIdx function broke, this should be unreachable.");
	}

	// =====================================================================================
	//
	// MATH ALGORITHMS
	//
	// =====================================================================================
	//from http://www.ryanjuckett.com/programming/damped-springs/
	// zeta is damping ratio, omega is angular frequency
	public static void StepSpringSimulation_Harmonic( ref float pos, ref float vel, float zeta, float omega, float dt )
	{
		float oldPos = pos;
		float oldVel = vel;

		if ( zeta > 1.001f )
		{
			//overdamped
			float za = -omega * zeta;
			float zb = omega * Mathf.Sqrt( zeta * zeta - 1f );
			float z1 = za - zb;
			float z2 = za + zb;


			float c1 = (oldVel - oldPos * z2) / (z1 - z2);
			float c2 = oldPos - c1;

			float e_z1dt = Mathf.Exp( z1 * dt );
			float e_z2dt = Mathf.Exp( z2 * dt );

			pos = c1 * e_z1dt + c2 * e_z2dt;
			vel = c1 * z1 * e_z1dt + c2 * z2 *e_z2dt;
		}
		else if ( zeta < 0.999f )
		{
			//underdamped
			float alpha = omega *  Mathf.Sqrt( 1 - zeta * zeta );
			float e_negOmegaZetaDt = Mathf.Exp( -omega * zeta * dt );

			float c1 = oldPos;
			float c2 = (oldVel + omega * zeta * oldPos) / alpha;

			pos = e_negOmegaZetaDt * (c1 * Mathf.Cos( alpha * dt ) + c2 * Mathf.Sin( alpha * dt ));
			vel = -e_negOmegaZetaDt * ((c1 * omega * zeta - c2 * alpha) * Mathf.Cos( alpha * dt ) + (c1 * alpha + c2 * omega * zeta) * Mathf.Sin( alpha * dt ));
		}
		else
		{
			//critically damped
			float a = (oldVel + oldPos * omega) * dt;
			float e_negOmegaDt = Mathf.Exp( -omega * dt );

			pos = (a + oldPos) * e_negOmegaDt;
			vel = (oldVel - a * omega) * e_negOmegaDt;
		}
	}

	public static void StepSpringSimulation_ConstantAndDamping( ref float pos, ref float vel, float constant, float damping, float dt, bool forceCriticalDamping = false )
	{
		float omega = Mathf.Sqrt( constant );
		float zeta = forceCriticalDamping ? 1f : damping / (2 * omega);
		StepSpringSimulation_Harmonic( ref pos, ref vel, zeta, omega, dt );
	}
}
