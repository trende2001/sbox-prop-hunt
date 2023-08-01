using Sandbox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;

namespace MyGame;

partial class Player : AnimatedEntity
{
	private BaseTeam team { get; set; } = null;
	public BaseTeam Team
	{
		get
		{
			if ( Game.IsClient )
			{
				return Teams.GetByName( TeamName );
			}

			return team;
		}
		set
		{
			if ( Game.IsClient ) return;
			team = value;
			TeamName = value?.TeamName;
		}
	}
	[Net] public string TeamName { get; set; }
	[Net] public Color TeamColour { get; set; }
	
	public bool IsSpectator
	{
		get => Client.GetValue<bool>( "spectator", false ) || (PropHuntGame.Current.RoundState == RoundState.Started && TeamName == "Spectator");
		set => Client.SetValue( "spectator", value );
	}

	private float MaxHealth = 100f;
	
	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		Event.Run( "Player.PreSpawn", this );
		base.Spawn();
		Velocity = Vector3.Zero;
		Components.RemoveAll();
		LifeState = LifeState.Alive;
		Health = 100;

		SetModel( "models/citizen/citizen.vmdl" );
		
		PopupSystem.DisplayPopup( To.Everyone, "Hide or die", "The seekers will be unblinded in 15 seconds", 15f );

		Components.Add( new WalkController() );
		Components.Add( new FirstPersonCamera() );
		Components.Add( new AmmoStorageComponent() );
		Components.Add( new InventoryComponent() );
		Components.Add( new CitizenAnimationComponent() );
		Components.Add( new UseComponent() );
		Components.Add( new UnstuckComponent() );
		Ammo.ClearAmmo();
		CreateHull();
		Tags.Add( "player" );
		Tags.Remove( "prop" );
		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableTouch = true;
		EnableLagCompensation = true;
		Predictable = true;
		EnableHitboxes = true;

		MoveToSpawnpoint();
		Event.Run( "Player.PostSpawn", this );
	}

	/// <summary>
	/// Respawn this player.
	/// </summary>
	/// 
	public virtual void Respawn()
	{
		Event.Run( "Player.PreRespawn", this );
		Spawn();
		RemoveViewmodelRPC(To.Single( this ));
		Tags.Remove( "prop" );
		Event.Run( "Player.PostRespawn", this );
	}

	public virtual void MoveToSpawnpoint()
	{
		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			Transform = tx;
		}
	}
	// An example BuildInput method within a player's Pawn class.
	[ClientInput] public Vector3 InputDirection { get; set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public bool LockRotation { get; private set; }

	public MovementComponent MovementController => Components.Get<MovementComponent>();
	public CameraComponent CameraController => Components.Get<CameraComponent>();
	public AnimationComponent AnimationController => Components.Get<AnimationComponent>();
	public InventoryComponent Inventory => Components.Get<InventoryComponent>();
	public AmmoStorageComponent Ammo => Components.Get<AmmoStorageComponent>();
	public UseComponent UseKey => Components.Get<UseComponent>();
	public UnstuckComponent UnstuckController => Components.Get<UnstuckComponent>();


	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

	public BBox Hull
	{
		get => new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 72 )
		);
	}

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );
	/// <summary>
	/// Create a physics hull for this player. The hull stops physics objects and players passing through
	/// the player. It's basically a big solid box. It also what hits triggers and stuff.
	/// The player doesn't use this hull for its movement size.
	/// </summary>
	public virtual void CreateHull()
	{
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, Hull.Mins, Hull.Maxs );

		//	phys.GetBody(0).RemoveShadowController();

		// TODO - investigate this? if we don't set movetype then the lerp is too much. Can we control lerp amount?
		// if so we should expose that instead, that would be awesome.
		EnableHitboxes = true;
	}
	DamageInfo LastDamage;
	public override void TakeDamage( DamageInfo info )
	{
		if ( Game.IsClient ) return;
		Event.Run( "Player.PreTakeDamage", info, this );
		LastDamage = info;
		LastAttacker = info.Attacker;
		LastAttackerWeapon = info.Weapon;
		if ( Health > 0f && LifeState == LifeState.Alive )
		{
			Health -= info.Damage;
			if ( Health <= 0f )
			{
				Health = 0f;
				OnKilled();
			}
		}
		Event.Run( "Player.PostTakeDamage", info, this );
	}
	public override void OnKilled()
	{
		if ( Game.IsClient ) return;
		Event.Run( "Player.PreOnKilled", this );
		LifeState = LifeState.Dead;
		BecomeRagdoll( LastDamage );
		RemoveViewmodelRPC(To.Single( this ));

		Inventory.ActiveChild = null;
		Inventory.ActiveChildInput = null;
		if ( Game.IsServer )
		{
			EnableAllCollisions = false;
			EnableDrawing = false;
			Inventory.DropItem( Inventory.ActiveChild );
			foreach ( var item in Inventory.Items.ToList() )
			{
				Inventory.DropItem( item );
			}
			Inventory.Items.Clear();
			Components.Add( new NoclipController() );
		}
		Event.Run( "Player.PostOnKilled", this );
	}

	[ClientRpc]
	private void RemoveViewmodelRPC()
	{
		foreach ( var viewmodel in BaseViewModel.AllViewModels.ToList() )
		{
			viewmodel.Delete();
		}
	}

	//---------------------------------------------// 

	/// <summary>
	/// Pawns get a chance to mess with the input. This is called on the client.
	/// </summary>
	public override void BuildInput()
	{
		base.BuildInput();
		// these are to be done in order and before the simulated components
		CameraController?.BuildInput();
		MovementController?.BuildInput();
		AnimationController?.BuildInput();

		foreach ( var i in Components.GetAll<SimulatedComponent>() )
		{
			if ( i.Enabled ) i.BuildInput();
		}
	}

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
		// toggleable third person
		if ( Input.Pressed( "View" ) && Game.IsServer )
		{
			if ( CameraController is FirstPersonCamera )
			{
				Components.Add( new ThirdPersonCamera() );
			}
			else if ( CameraController is ThirdPersonCamera )
			{
				Components.Add( new FirstPersonCamera() );
			}
		}

		if ( Team is Props )
		{
			if ( Input.Pressed( "use" ) && Game.IsServer )
			{

				var tr = Trace.Ray( Camera.Position, Camera.Position + EyeRotation.Forward * 115f )
					.UseHitboxes( true )
					.Ignore( this )
					.Run();

				DebugOverlay.TraceResult( tr, 5f );


				if ( tr.Hit && tr.Entity is Prop prop && tr.Body.IsValid() &&
				     tr.Body.BodyType == PhysicsBodyType.Dynamic )
				{
					ChangeIntoProp( prop );
					Sound.FromEntity( To.Single( this ), "player_use", this );
				}
			}

			if ( Input.Pressed( "Attack1" ) && Game.IsServer )
			{
				LockRotation = !LockRotation;
			}

			if ( Input.Pressed( "Attack2" ) && Game.IsServer )
			{
				Vector3 startPos = Position;
				startPos += Vector3.Up * (CollisionBounds.Maxs.z * Scale) * 0.75f;

				var tr = Trace.Ray( startPos, startPos + EyeRotation.Forward * 100 )
					.UseHitboxes( true )
					.Ignore( this )
					.Run();

				DebugOverlay.TraceResult( tr, 5f );


				if ( tr.Hit && tr.Entity.IsValid && tr.Entity is not Player )
				{
					var DmgInfo = DamageInfo.FromBullet( tr.EndPosition, Velocity * tr.Direction, 10000f )
						.UsingTraceResult( tr )
						.WithAttacker( this );

					tr.Entity.TakeDamage( DmgInfo );
				}
			}
			
			if ( Input.Pressed( "Flashlight" ) && Game.IsServer )
			{
				var sound = PlaySound( "random_taunts" );
				sound.SetVolume( 1.9f );
			}
		}

		if ( Game.IsClient )
		{
			if ( Input.MouseWheel > 0.1 )
			{
				Inventory?.SwitchActiveSlot( 1, true );
			}

			if ( Input.MouseWheel < -0.1 )
			{
				Inventory?.SwitchActiveSlot( -1, true );
			}
		}

		// these are to be done in order and before the simulated components
		UnstuckController?.Simulate( cl );
		MovementController?.Simulate( cl );
		CameraController?.Simulate( cl );
		AnimationController?.Simulate( cl );
		foreach ( var i in Components.GetAll<SimulatedComponent>() )
		{
			if ( i.Enabled ) i.Simulate( cl );
		}
	}

	private void ChangeIntoProp( Prop prop )
	{
		SetModel( prop.GetModelName() );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, prop.CollisionBounds.Mins, prop.CollisionBounds.Maxs );
		
		Tags.Add( "prop" );

		EnableHitboxes = true;

		Scale = prop.Scale;
		RenderColor = prop.RenderColor;
		CollisionBounds = prop.CollisionBounds; 
		HitboxSet = prop.HitboxSet;
		
		SetMaterialGroup( prop.GetMaterialGroup() );

		Components.Add( new PropController() );
		Components.Add( new PropAnimator() );
		Components.Remove( new CitizenAnimationComponent() );

		Clothing.ClearEntities();

		// Calculate the health based on the volume that the chosen model encompasses.
		// Clamp our current health and maximum health between 0 and 1, so we can't go over the imposed limit.
		// Bring our prop collison volume to the power of 0.5 (y) then multiply it by 0.5

		float multiplier = Math.Clamp( Health / MaxHealth, 0, 1 );
		float health = (float)Math.Pow( prop.CollisionBounds.Volume, 0.5f ) * 0.5f;

		health = (float)Math.Round( health / 5 ) * 5;
		MaxHealth = health;
		Health = health * multiplier;

		if( Children.Any() )
		{
			for ( int i = 0; i < Children.Count; i++ )
			{
				Children[i].Delete();
			}
		}


		// Does the prop have any children?
		if ( prop.Children.Any() && Game.IsServer )
		{
			Log.Error( $"This prop has {prop.Children.Count()} children." );

			
			// THERE HAS TO BE A BETTER WAY TO DO THIS.
			foreach ( var child in prop.Children )
			{
				if ( child is PointLightEntity ChildPointLight )
				{
					var LightEntity = new PointLightEntity();
						LightEntity.Parent = this;
						LightEntity.LocalPosition = ChildPointLight.LocalPosition;
						LightEntity.LocalRotation = ChildPointLight.LocalRotation;
						LightEntity.Position = this.Position;
						LightEntity.Predictable = true;
							
						LightEntity.Brightness = ChildPointLight.Brightness;
						LightEntity.BrightnessMultiplier = ChildPointLight.BrightnessMultiplier;
						LightEntity.Color = ChildPointLight.Color;
						LightEntity.Flicker = ChildPointLight.Flicker;
						LightEntity.Range = ChildPointLight.Range;
						LightEntity.DynamicShadows = ChildPointLight.DynamicShadows;
						LightEntity.FadeDistanceMax = ChildPointLight.FadeDistanceMax;
						LightEntity.FadeDistanceMin = ChildPointLight.FadeDistanceMin;
						LightEntity.LightSize = ChildPointLight.LightSize;
				}
				else if ( child is SpotLightEntity ChildSpotLight )
				{
					var LightEntity = new SpotLightEntity();
					
						LightEntity.Parent = this;
						LightEntity.Position = this.Position;
						LightEntity.LocalPosition = ChildSpotLight.LocalPosition;
						LightEntity.LocalRotation = ChildSpotLight.LocalRotation;
						LightEntity.Predictable = true;
						
						LightEntity.Brightness = ChildSpotLight.Brightness;
						LightEntity.BrightnessMultiplier = ChildSpotLight.BrightnessMultiplier;
						LightEntity.Color = ChildSpotLight.Color;
						LightEntity.Flicker = ChildSpotLight.Flicker;
						LightEntity.Range = ChildSpotLight.Range;
						LightEntity.DynamicShadows = ChildSpotLight.DynamicShadows;
						LightEntity.FadeDistanceMax = ChildSpotLight.FadeDistanceMax;
						LightEntity.FadeDistanceMin = ChildSpotLight.FadeDistanceMin;
						LightEntity.InnerConeAngle = ChildSpotLight.InnerConeAngle;
						LightEntity.OuterConeAngle = ChildSpotLight.OuterConeAngle;
						LightEntity.LightCookie = ChildSpotLight.LightCookie;
					
				}
			}
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
		// these are to be done in order and before the simulated components
		UnstuckController?.FrameSimulate( cl );
		MovementController?.FrameSimulate( cl );
		CameraController?.FrameSimulate( cl );
		AnimationController?.FrameSimulate( cl );
		foreach ( var i in Components.GetAll<SimulatedComponent>() )
		{
			if ( i.Enabled ) i.FrameSimulate( cl );
		}
	}
	TimeSince timeSinceLastFootstep = 0;
	public override void OnAnimEventFootstep( Vector3 position, int foot, float volume )
	{
		if ( LifeState != LifeState.Alive )
			return;

		if ( Game.IsServer )
			return;

		if ( timeSinceLastFootstep < 0.2f )
			return;
		volume *= FootstepVolume();
		var tr = Trace.Ray( position, position + Vector3.Down * 20 ).Radius( 1 ).Ignore( this ).Run();
		if ( !tr.Hit ) return;
		timeSinceLastFootstep = 0;
		tr.Surface.DoFootstep( this, tr, foot, volume * 10 );
	}

	public virtual float FootstepVolume()
	{
		if ( MovementController is WalkController wlk )
		{
			if ( wlk.IsDucking ) return 0.3f;
		}
		return 1;
	}
}
