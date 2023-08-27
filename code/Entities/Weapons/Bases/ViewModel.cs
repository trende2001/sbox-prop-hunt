using Sandbox;
using System;

namespace MyGame;

public class ViewModel : BaseViewModel
{
	public Carriable Carriable { get; set; }
	
	[ConVar.Client] 
	public static float cl_bob { get; set; } = 0.01f;
	
	[ConVar.Client] 
	public static float cl_bobcycle { get; set; } = 0.8f;
	
	[ConVar.Client] 
	public static float cl_bobup { get; set; } = 0.5f;
	
	private float g_fMaxViewModelLag = 1.5f;//1.5f;
	private float ViewModelLagScale = 1.1f; // normally 5
	private float ViewModelLagSpeed = 5f; // normally 5
	
	private float bob;
	private float bobv;
	
	private float bobtimevm;
	private float lastbobtimevm;

	private float HL2_BOB_CYCLE_MIN = 1.0f;
	private float HL2_BOB_CYCLE_MAX = 0.45f;
	private float HL2_BOB = 0.002f;
	private float HL2_BOB_UP = 0.5f;

	private Vector3 m_vecLastFacing;
	private Vector3 m_vecLastPos;

	public override void PlaceViewmodel()
	{
		if ( Game.IsRunningInVR )
			return;
		
		Position = Camera.Position;
		Rotation = Camera.Rotation;
		
		CalcViewmodelLag();
		AddViewmodelBob();
	}
	
	/// <summary>
	/// Change the magnitude of a vector while keeping its direction unchanged
	/// </summary>
	/// <param name="va">Point A</param>
	/// <param name="scale">Product of scale</param>
	/// <param name="vb">Point B</param>
	/// <returns>Result of multiplication</returns>
	public static Vector3 VectorMagnitude( Vector3 va, float scale, Vector3 vb )
	{
		Vector3 vc = Vector3.Zero;
		vc[0] = va[0] + scale * vb[0];
		vc[1] = va[1] + scale * vb[1];
		vc[2] = va[2] + scale * vb[2];
		return vc;
	}

	public static float RemapValue( float val, float a, float b, float c, float d )
	{
		if ( a == b )
			return val >= b ? d : c;
		return c + (d - c) * (val - a) / (b - a);
	}

	private void CalcViewmodelLag()
	{
		if ( Game.LocalPawn is not Player player ) return;
		var viewmodel = this;
		Rotation originalAngles = viewmodel.Rotation;

		Vector3 vOriginalOrigin = viewmodel.Position;
		Rotation vOriginalAngles = viewmodel.Rotation;
		// Calculate our drift
		Vector3 forward = viewmodel.Rotation.Forward;

		if ( Time.Delta != 0.0f )
		{
			Vector3 vDifference = forward - m_vecLastFacing;

			float flSpeed = ViewModelLagSpeed;
			// If we start to lag too far behind, we'll increase the "catch up" speed.  Solves the problem with fast cl_yawspeed, m_yaw or joysticks
			//  rotating quickly.  The old code would slam lastfacing with origin causing the viewmodel to pop to a new position

			float flDiff = vDifference.Length;
			if ( (flDiff > g_fMaxViewModelLag) && (g_fMaxViewModelLag > 0.0f) )
			{
				float flScale = flDiff / g_fMaxViewModelLag;
				flSpeed *= flScale;
			}

			// FIXME:  Needs to be predictable?
			m_vecLastFacing = VectorMagnitude( m_vecLastFacing, flSpeed * Time.Delta, vDifference );
			// Make sure it doesn't grow out of control!!!
			m_vecLastFacing = m_vecLastFacing.Normal;

			viewmodel.Position = VectorMagnitude( viewmodel.Position, ViewModelLagScale, vDifference * -1.0f );
			//Assert( m_vecLastFacing.IsValid() );
		}
		forward = originalAngles.Forward;
		Vector3 right = originalAngles.Right;
		Vector3 up = originalAngles.Up;

		float pitch = originalAngles.Pitch();
		if ( pitch > 180.0f )
			pitch -= 360.0f;
		else if ( pitch < -180.0f )
			pitch += 360.0f;

		if ( g_fMaxViewModelLag == 0.0f )
		{
			viewmodel.Position = vOriginalOrigin;
			viewmodel.Rotation = vOriginalAngles;
		}
		Vector3 vel = new Vector3( player.Velocity.x, player.Velocity.y );
		float speed = vel.Length;
	}

	private void AddViewmodelBob()
	{
		var viewmodel = this;
		Vector3 forward = viewmodel.Rotation.Forward;
		Vector3 right = viewmodel.Rotation.Right;

		CalcViewmodelBob();

		float lateralBob = bob;
		float verticalBob = bobv;

		// Apply bob, but scaled down to 40%
		viewmodel.Position = VectorMagnitude( viewmodel.Position, verticalBob * 0.1f, forward );

		// Z bob a bit more
		var a = viewmodel.Position;
		a[2] += verticalBob * 0.1f;
		viewmodel.Position = a;

		// bob the angles

		viewmodel.Rotation = viewmodel.Rotation.Angles().WithRoll( viewmodel.Rotation.Angles().roll + verticalBob * 0.5f ).ToRotation();
		viewmodel.Rotation = viewmodel.Rotation.Angles().WithPitch( viewmodel.Rotation.Angles().pitch - verticalBob * 0.4f ).ToRotation();
		viewmodel.Rotation = viewmodel.Rotation.Angles().WithYaw( viewmodel.Rotation.Angles().yaw - lateralBob * 0.3f ).ToRotation();


		viewmodel.Position = VectorMagnitude( viewmodel.Position, lateralBob * 0.8f, right );
	}

	private void CalcViewmodelBob()
	{
		float cycle;

		if ( Game.LocalPawn is not Player player ) 
			return;

		//Find the speed of the player
		Vector3 vel = new Vector3( player.Velocity.x, player.Velocity.y );
		float speed = vel.Length;
		

		speed = Math.Clamp( speed, -320, 320 );

		float bob_offset = RemapValue( speed, 0, 320, 0.0f, 1.0f );

		bobtimevm += (Time.Now - lastbobtimevm) * bob_offset;
		lastbobtimevm = Time.Now;

		//Calculate the vertical bob
		cycle = bobtimevm - (int)(bobtimevm / HL2_BOB_CYCLE_MAX) * HL2_BOB_CYCLE_MAX;
		cycle /= HL2_BOB_CYCLE_MAX;

		if ( cycle < HL2_BOB_UP )
		{
			cycle = MathF.PI * cycle / HL2_BOB_UP;
		}
		else
		{
			cycle = MathF.PI + MathF.PI * (cycle - HL2_BOB_UP) / (1.0f - HL2_BOB_UP);
		}

		bobv = speed * 0.005f;
		bobv = bobv * 0.3f + bobv * 0.7f * MathF.Sin( cycle );

		bobv = Math.Clamp( bobv, -7.0f, 4.0f );

		//Calculate the lateral bob
		cycle = bobtimevm - (int)(bobtimevm / HL2_BOB_CYCLE_MAX * 2) * HL2_BOB_CYCLE_MAX * 2;
		cycle /= HL2_BOB_CYCLE_MAX * 2;

		if ( cycle < HL2_BOB_UP )
		{
			cycle = MathF.PI * cycle / HL2_BOB_UP;
		}
		else
		{
			cycle = MathF.PI + MathF.PI * (cycle - HL2_BOB_UP) / (1.0f - HL2_BOB_UP);
		}

		bob = speed * 0.005f;
		bob = bob * 0.3f + bob * 0.7f * MathF.Sin( cycle );
		bob = Math.Clamp( bob, -7.0f, 4.0f );
	}
}
