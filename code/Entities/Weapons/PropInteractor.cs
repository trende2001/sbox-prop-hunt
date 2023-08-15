namespace MyGame;

using Sandbox;
using System;

public class PropInteractor : Carriable
{
	public override Model ViewModel => Cloud.Model( "https://asset.party/facepunch/v_usp" );
	public override Model WorldModel => Cloud.Model( "https://asset.party/facepunch/w_usp" );

	public PhysicsBody HeldBody { get; private set; }
	public Vector3 HeldPos { get; private set; }
	public Rotation HeldRot { get; private set; }
	public ModelEntity HeldEntity { get; private set; }
	public Vector3 HoldPos { get; private set; }
	public Rotation HoldRot { get; private set; }

	protected virtual float MaxPullDistance => 2000.0f;
	protected virtual float PullForce => 20.0f;
	protected virtual float HoldDistance => 50.0f;
	protected virtual float AttachDistance => 150.0f;
	protected virtual float DropCooldown => 0.5f;

	private TimeSince timeSinceDrop;

	private const string grabbedTag = "grabbed";
	
	public override void Spawn()
	{
		base.Spawn();
		
		Tags.Add( "weapon" );
	}


	[GameEvent.Entity.PreCleanup]
	protected void OnEntityPreCleanup()
	{
		GrabEnd();
	}

	public override void Simulate( IClient client )
	{
		if ( Owner is not Player owner ) return;

		if ( !Game.IsServer )
			return;

		using ( Prediction.Off() )
		{
			var eyePos = owner.EyePosition;
			var eyeRot = owner.EyeRotation;
			var eyeDir = owner.EyeRotation.Forward;

			if ( HeldBody.IsValid() && HeldBody.PhysicsGroup != null )
			{
				if ( Input.Pressed( "attack1" ) )
				{
					GrabEnd();
				}
				else
				{
					GrabMove( eyePos, eyeDir, eyeRot );
				}

				return;
			}

			if ( timeSinceDrop < DropCooldown )
				return;

			var tr = Trace.Ray( eyePos, eyePos + eyeDir * MaxPullDistance )
				.UseHitboxes()
				.WithAnyTags( "solid", "debris" )
				.Ignore( this )
				.Radius( 2.0f )
				.Run();

			if ( !tr.Hit || !tr.Body.IsValid() || !tr.Entity.IsValid() || tr.Entity.IsWorld )
				return;

			if ( tr.Entity.PhysicsGroup == null )
				return;

			var modelEnt = tr.Entity as ModelEntity;
			if ( !modelEnt.IsValid() )
				return;

			if ( modelEnt.Tags.Has( grabbedTag ) )
				return;

			var body = tr.Body;

			if ( body.BodyType != PhysicsBodyType.Dynamic )
				return;
			
			if ( Input.Down( "attack1" ) )
			{
				var physicsGroup = tr.Entity.PhysicsGroup;

				if ( physicsGroup.BodyCount > 1 )
				{
					body = modelEnt.PhysicsBody;
					if ( !body.IsValid() )
						return;
				}

				var attachPos = body.FindClosestPoint( eyePos );

				if ( eyePos.Distance( attachPos ) <= AttachDistance )
				{
					var holdDistance = HoldDistance + attachPos.Distance( body.MassCenter );
					GrabStart( modelEnt, body, eyePos + eyeDir * holdDistance, eyeRot );
				}
				else
				{
					physicsGroup.ApplyImpulse( eyeDir * -PullForce, true );
				}
			}
		}
	}

	private void Activate()
	{
	}

	private void Deactivate()
	{
		GrabEnd();
	}

	public override void OnActiveStart(  )
	{
		base.OnActiveStart( );

		if ( Game.IsServer )
		{
			Activate();
		}
	}

	public override void OnActiveEnd( )
	{
		base.OnActiveEnd();

		if ( Game.IsServer )
		{
			Deactivate();
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if ( Game.IsServer )
		{
			Deactivate();
		}
	}
	
	[GameEvent.Physics.PreStep]
	public void OnPrePhysicsStep()
	{
		if ( !Game.IsServer )
			return;

		if ( !HeldBody.IsValid() )
			return;

		if ( HeldEntity is Player )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.1f, Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.1f, Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	private void GrabStart( ModelEntity entity, PhysicsBody body, Vector3 grabPos, Rotation grabRot )
	{
		if ( !body.IsValid() )
			return;

		if ( body.PhysicsGroup == null )
			return;

		GrabEnd();

		HeldBody = body;
		HeldPos = HeldBody.LocalMassCenter;
		HeldRot = grabRot.Inverse * HeldBody.Rotation;

		HoldPos = HeldBody.Position;
		HoldRot = HeldBody.Rotation;

		HeldBody.Sleeping = false;
		HeldBody.AutoSleep = false;

		HeldEntity = entity;
		HeldEntity.Tags.Add( grabbedTag );

		Client?.Pvs.Add( HeldEntity );
	}

	private void GrabEnd()
	{
		timeSinceDrop = 0;

		if ( HeldBody.IsValid() )
		{
			HeldBody.AutoSleep = true;
		}

		if ( HeldEntity.IsValid() )
		{
			Client?.Pvs.Remove( HeldEntity );
		}

		HeldBody = null;
		HeldRot = Rotation.Identity;

		if ( HeldEntity.IsValid() )
		{
			HeldEntity.Tags.Remove( grabbedTag );
		}

		HeldEntity = null;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot )
	{
		if ( !HeldBody.IsValid() )
			return;

		var attachPos = HeldBody.FindClosestPoint( startPos );
		var holdDistance = HoldDistance + attachPos.Distance( HeldBody.MassCenter );

		HoldPos = startPos - HeldPos * HeldBody.Rotation + dir * holdDistance;
		HoldRot = rot * HeldRot;
	}
}
