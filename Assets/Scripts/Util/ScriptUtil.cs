using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ScriptUtil
{
	// =====================================================================================
	//
	// SHARED ENUMS
	//
	// =====================================================================================
	public enum CompareType { PASS, FAIL, LESS_THAN, LESS_THAN_OR_EQUAL_TO, EQUAL, GREATER_THAN, GREATER_THAN_OR_EQUAL_TO }
	public static bool Compare<T>( T lhs, T rhs, CompareType compareType ) where T : System.IComparable
	{
		switch ( compareType )
		{
			case CompareType.PASS:
				return true;
			case CompareType.FAIL:
				return false;
			case CompareType.LESS_THAN:
				return lhs.CompareTo( rhs ) < 0;
			case CompareType.LESS_THAN_OR_EQUAL_TO:
				return lhs.CompareTo( rhs ) <= 0;
			case CompareType.EQUAL:
				return lhs.CompareTo( rhs ) == 0;
			case CompareType.GREATER_THAN:
				return lhs.CompareTo( rhs ) > 0;
			case CompareType.GREATER_THAN_OR_EQUAL_TO:
				return lhs.CompareTo( rhs ) >= 0;
			default:
				throw new UnityException( "Unhandled compare type: " + compareType );
		}
	}

	// =====================================================================================
	//
	// ALGORITHMS
	//
	// =====================================================================================
	//from https://gamedev.stackexchange.com/questions/149103/why-use-time-deltatime-in-lerping-functions
	public static float GetAsymptoticLerpFrac( float baseFrac, float dt, float referenceFramerate = 30f )
	{
		return 1f - Mathf.Pow( 1f - baseFrac, dt * referenceFramerate );
	}
	public static float AsymptoticLerp( float start, float target, float baseFrac, float dt )
	{
		return Mathf.Lerp( start, target, GetAsymptoticLerpFrac( baseFrac, dt ) );
	}
	public static Vector3 AsymptoticLerp( Vector3 start, Vector3 target, float baseFrac, float dt )
	{
		return Vector3.Lerp( start, target, GetAsymptoticLerpFrac( baseFrac, dt ) );
	}

	public static void Shuffle<T>( this IList<T> list )
	{
		int n = list.Count;
		while ( n > 1 )
		{
			n--;
			int k = UnityEngine.Random.Range( 0, n + 1 );
			T value = list[k];
			list[ k ] = list[ n ];
			list[ n ] = value;
		}
	}

	public static Vector3 GetRandomVectorInCone( Vector3 forward, float halfAngle, Vector3 referenceUp )
	{
		Vector3 right = Vector3.Cross( forward, referenceUp ).normalized;
		Vector3 res = forward;

		float rand1 = UnityEngine.Random.Range( 0f, halfAngle );
		float rand2 = UnityEngine.Random.Range( 0f, 360f );
		res = Quaternion.AngleAxis( rand1, right ) * res;
		res = Quaternion.AngleAxis( rand2, forward ) * res;

		return res;
	}

	public static Vector3 GetRandomVectorInCone( Vector3 forward, float halfAngle )
	{
		return GetRandomVectorInCone( forward, halfAngle, Vector3.up );
	}

	public static Vector3 GetRandomVectorOnConeEdge( Vector3 forward, float halfAngle, Vector3 referenceUp )
	{
		Vector3 right = Vector3.Cross( forward, referenceUp ).normalized;
		Vector3 res = forward;

		float rand2 = UnityEngine.Random.Range( 0f, 360f );
		res = Quaternion.AngleAxis( halfAngle, right ) * res;
		res = Quaternion.AngleAxis( rand2, forward ) * res;

		return res;
	}

	public static Vector3 GetRandomVectorOnConeEdge( Vector3 forward, float halfAngle )
	{
		return GetRandomVectorOnConeEdge( forward, halfAngle, Vector3.up );
	}

	// =====================================================================================
	//
	// HANDY HELPER FUNCS
	//
	// =====================================================================================
	public static IEnumerable<T> GetIterableEnumValues<T>()
	{
		return Enum.GetValues( typeof( T ) ).Cast<T>();
	}

	public static T[] InitializeArray<T>( int length ) where T : new()
	{
		T[] array = new T[length];
		for ( int i = 0; i < length; i++ )
		{
			array[i] = new T();
		}

		return array;
	}

	public static Quaternion ClampRotationAroundXAxis( Quaternion q, float minX, float maxX )
	{
		q.x /= q.w;
		q.y /= q.w;
		q.z /= q.w;
		q.w = 1.0f;

		float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

		angleX = Mathf.Clamp( angleX, minX, maxX );

		q.x = Mathf.Tan( 0.5f * Mathf.Deg2Rad * angleX );

		return q;
	}

	public static float EvaluateFloatOverTimeVar( FloatOverTime fot, float time )
	{
		switch ( fot.type )
		{
			case FloatOverTime.ChangeTypes.CONST:
				return fot.value;
			case FloatOverTime.ChangeTypes.PERLIN_NOISE:
				time += fot.timeStartRandOffset;
				float perlin = Mathf.PerlinNoise( time * fot.noisePanSpeedHorizontal, time * fot.noisePanSpeedVertical );
				if ( fot.mirroredAround0 )
					perlin = MathUtil.RemapFrom01( perlin, -1f, 1f );
				return perlin * fot.noiseMultiplier;
			case FloatOverTime.ChangeTypes.CUSTOM_NOISE_TEXTURE:
				time += fot.timeStartRandOffset;
				float texVal = fot.noiseTexture1d.GetPixel( Mathf.FloorToInt( (time * fot.noisePanSpeedHorizontal / fot.noiseTexture1d.width) % fot.noiseTexture1d.width ),
					Mathf.FloorToInt( (time * fot.noisePanSpeedVertical / fot.noiseTexture1d.height) % fot.noiseTexture1d.height ) ).r;
				if ( fot.mirroredAround0 )
					texVal = MathUtil.RemapFrom01( texVal, -1f, 1f );
				return texVal * fot.noiseMultiplier;
			case FloatOverTime.ChangeTypes.SINUSOID:
				time += fot.timeStartRandOffset;
				return fot.amplitude * Mathf.Sin( (2 * Mathf.PI / fot.period) * (time + fot.phaseShift) ) + fot.verticalShift;
			case FloatOverTime.ChangeTypes.CUSTOM_CURVE:
				if ( fot.useTimeRandOffset )
					time += fot.timeStartRandOffset;
				time = MathUtil.RemapTo01( time, 0f, fot.curveXMax );
				return fot.curve.Evaluate( time ) * fot.curveYMultiplier;
			default:
				throw new UnityException( "Unhandled FloatOverTime Type " + fot.type );
		}
	}

	public static Color SetColorAlpha( Color inputColor, float alpha )
	{
		Color col = inputColor;
		col.a = alpha;
		return col;
	}

	public static Vector3 CalcHitboxConeScaleForParameters( Vector3 originalScale, float desiredDist, float desiredHalfAngleHorizontal, float desiredHalfAngleVertical )
	{
		//assume cone mesh is dist 1 and half angle of 45degrees for easy scaling math. 
		//	Dist ratio = desiredDist / (meshDist=1) = desiredDist
		//	desiredWidth = desiredDist * sin(desiredHalfAngle)
		//	width ratio = desiredWidth / (sqrt(2)/2) since 1m dist sphere cone has width sqrt(2)/2 from dist=1 * sin(45)=sqrt(2)/2
		//		= desiredDist * sin(desiredHalfAngle) / (sqrt(2)/2)
		float z = originalScale.z * desiredDist;
		float x = originalScale.x * desiredDist * Mathf.Sin( desiredHalfAngleHorizontal * Mathf.Deg2Rad ) / MathUtil.SQRT2OVER2;
		float y = originalScale.y * desiredDist * Mathf.Sin( desiredHalfAngleVertical * Mathf.Deg2Rad ) / MathUtil.SQRT2OVER2;
		return new Vector3( x, y, z );
	}

	public static Vector3 CalcHitboxConeScaleForParameters( Transform meshTransform, float desiredDist, float desiredHalfAngleHorizontal, float desiredHalfAngleVertical )
	{
		return CalcHitboxConeScaleForParameters( meshTransform.localScale, desiredDist, desiredHalfAngleHorizontal, desiredHalfAngleVertical );
	}
}

// =====================================================================================
//
// DATA TYPES
//
// =====================================================================================
[Serializable]
public class FloatOverTime
{
	public enum ChangeTypes { CONST, PERLIN_NOISE, CUSTOM_NOISE_TEXTURE, SINUSOID, CUSTOM_CURVE }
	public ChangeTypes type;
	[HideInInspector] public float timeStartRandOffset;

	//const
	[ShowIf("type", ChangeTypes.CONST)]
	public float value;

	//noise
	[ShowIf("@this.type == ChangeTypes.PERLIN_NOISE || this.type == ChangeTypes.CUSTOM_NOISE_TEXTURE")]
	public float noisePanSpeedHorizontal;
	[ShowIf("@this.type == ChangeTypes.PERLIN_NOISE || this.type == ChangeTypes.CUSTOM_NOISE_TEXTURE")]
	public float noisePanSpeedVertical;
	[ShowIf("@this.type == ChangeTypes.PERLIN_NOISE || this.type == ChangeTypes.CUSTOM_NOISE_TEXTURE")]
	public bool mirroredAround0;
	[ShowIf("@this.type == ChangeTypes.PERLIN_NOISE || this.type == ChangeTypes.CUSTOM_NOISE_TEXTURE")]
	public float noiseMultiplier = 1f;
	[ShowIf( "type", ChangeTypes.CUSTOM_NOISE_TEXTURE )]
	[InlineEditor(InlineEditorModes.LargePreview)]
	public Texture2D noiseTexture1d;

	//sinusoid
	[ShowIf( "type", ChangeTypes.SINUSOID )]
	public float amplitude;
	[ShowIf( "type", ChangeTypes.SINUSOID )]
	public float period;
	[ShowIf( "type", ChangeTypes.SINUSOID )]
	public float phaseShift;
	[ShowIf( "type", ChangeTypes.SINUSOID )]
	public float verticalShift;


	//custom curve
	[ShowIf( "type", ChangeTypes.CUSTOM_CURVE )]
	public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
	[ShowIf( "type", ChangeTypes.CUSTOM_CURVE )]
	public float curveXMax;
	[ShowIf( "type", ChangeTypes.CUSTOM_CURVE )]
	public float curveYMultiplier;
	[ShowIf( "type", ChangeTypes.CUSTOM_CURVE )]
	public bool useTimeRandOffset;
}

[Serializable]
public struct HitboxConeDefinition
{
	public float halfAngleHorizontal;
	public float halfAngleVertical;
	public float dist;
}

//so we can have sorted dictionary with duplicate float keys
class NonCollidingFloatComparer : Comparer<float>
{
	public override int Compare( float left, float right )
	{
		return (right > left) ? -1 : 1; // no zeroes 
	}
}