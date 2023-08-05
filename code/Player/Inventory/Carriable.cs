using Sandbox;
using System.Linq;

namespace MyGame;
/// <summary>
///  Something that can go into the player's inventory and have a worldmodel and viewmodel etc, 
/// </summary>
public partial class Carriable : AnimatedEntity
{

	/// <summary>
	/// Utility - return the entity we should be spawning particles from etc
	/// </summary>
	public virtual ModelEntity EffectEntity => (ViewModelEntity.IsValid() && IsFirstPersonMode) ? ViewModelEntity : this;
	public Entity Carrier { get; set; }
	[Net] public bool CanSwitchItems { get; protected set; } = true;
	public virtual Model WorldModel => null;
	public virtual Model ViewModel => null;
	public BaseViewModel ViewModelEntity { get; protected set; }
	
	public AnimatedEntity ViewModelArms { get; set; }

	public override void Spawn()
	{
		base.Spawn();
		CarriableSpawn();
	}
	internal virtual void CarriableSpawn()
	{
		Model = WorldModel;
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		EnableTouch = true;
	}
	/// <summary>
	/// Create the viewmodel. You can override this in your base classes if you want
	/// to create a certain viewmodel entity.
	/// </summary>
	public virtual void CreateViewModel()
	{
		Game.AssertClient();

		ViewModelEntity = new BaseViewModel();
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.Model = ViewModel;
		
		ViewModelEntity.SetBodyGroup( "barrel", 1 );
		ViewModelEntity.SetBodyGroup( "sights", 1 );

		ViewModelArms = new AnimatedEntity( "models/first_person/first_person_arms.vmdl" );
		ViewModelArms.SetParent( ViewModelEntity, true );
		ViewModelArms.EnableViewmodelRendering = true;
		
		CalcViewModelLag();
	}

	/// <summary>
	/// We're done with the viewmodel - delete it
	/// </summary>
	public virtual void DestroyViewModel()
	{
		ViewModelEntity?.Delete();
		ViewModelEntity = null;
	}
	
	[ConVar.Client] public static float cl_bob { get; set; } = 0.01f;
	[ConVar.Client] public static float cl_bobcycle { get; set; } = 0.8f;
	[ConVar.Client] public static float cl_bobup { get; set; } = 0.5f;
	
	float g_fMaxViewModelLag = 1.5f;//1.5f;
	float ViewModelLagScale = 1.1f; // normally 5
	float ViewModelLagSpeed = 5f; // normally 5

	Vector3 m_vecLastFacing;
	Vector3 m_vecLastPos;
	
	Vector3 VectorMA( Vector3 va, float scale, Vector3 vb )
	{
		Vector3 vc = Vector3.Zero;
		vc[0] = va[0] + scale * vb[0];
		vc[1] = va[1] + scale * vb[1];
		vc[2] = va[2] + scale * vb[2];
		return vc;
	}
	
	private void CalcViewModelLag()
	{
		if ( Game.LocalPawn is not Player player ) return;
		var viewmodel = this.ViewModelEntity;
		Rotation original_angles = viewmodel.Rotation;

		Vector3 vOriginalOrigin = viewmodel.Position;
		Rotation vOriginalAngles = viewmodel.Rotation;
		// Calculate our drift
		Vector3 forward = viewmodel.Rotation.Forward;
		//m_vecLastPos = Position - lastPos;
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
			m_vecLastFacing = VectorMA( m_vecLastFacing, flSpeed * Time.Delta, vDifference );
			// Make sure it doesn't grow out of control!!!
			m_vecLastFacing = m_vecLastFacing.Normal;

			viewmodel.Position = VectorMA( viewmodel.Position, ViewModelLagScale, vDifference * -1.0f );
			//Assert( m_vecLastFacing.IsValid() );
		}
		forward = original_angles.Forward;
		Vector3 right = original_angles.Right;
		Vector3 up = original_angles.Up;

		float pitch = original_angles.Pitch();
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

	public override void StartTouch( Entity other )
	{
		base.Touch( other );
		if ( other is Player ply )
		{
			if ( ply.Inventory?.Items.Where( x => x.GetType() == this.GetType() ).Count() <= 0 )
			{

				ply.Inventory?.AddItem( this );
			}
			else
			{
				if ( this is Weapon wep )
				{
					wep.PrimaryAmmo -= (int)(ply.Ammo?.GiveAmmo( wep.PrimaryAmmoType, wep.PrimaryAmmo ));
					wep.SecondaryAmmo -= (int)(ply.Ammo?.GiveAmmo( wep.SecondaryAmmoType, wep.SecondaryAmmo ));
					if ( wep.PrimaryAmmo <= 0 && wep.SecondaryAmmo <= 0 )
					{
						wep.Delete();
					}
				}
			}
		}
	}
	public virtual void OnPickup( Entity equipper )
	{
		SetParent( equipper, true );
		Owner = equipper;
		PhysicsEnabled = false;
		EnableAllCollisions = false;
		EnableDrawing = false;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}
	public virtual void OnDrop( Entity dropper )
	{
		SetParent( null );
		Owner = null;
		PhysicsEnabled = true;
		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = false;
		EnableShadowInFirstPerson = false;
	}
	public virtual void OnActiveStart()
	{
		EnableDrawing = true;
		if ( Game.IsClient )
		{
			DestroyViewModel();
			CreateViewModel();
		}
	}
	
	public virtual void OnActiveEnd()
	{
		if ( Parent is Player ) EnableDrawing = false;
		if ( Game.IsClient )
		{
			DestroyViewModel();
		}
	}
	public virtual void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.HoldType = CitizenAnimationHelper.HoldTypes.Pistol;
		anim.Handedness = CitizenAnimationHelper.Hand.Both;
		anim.AimBodyWeight = 1.0f;
	}
}
