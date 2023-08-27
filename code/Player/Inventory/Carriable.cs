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
	public ViewModel ViewModelEntity { get; protected set; }
	
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

		ViewModelEntity = new ViewModel();
		ViewModelEntity.Position = Position;
		ViewModelEntity.Owner = Owner;
		ViewModelEntity.EnableViewmodelRendering = true;
		ViewModelEntity.Model = ViewModel;
		ViewModelEntity.SetAnimParameter( "deploy", true );
		ViewModelEntity.SetAnimParameter( "b_deploy", true );

		ViewModelEntity.SetBodyGroup( "barrel", 1 );
		ViewModelEntity.SetBodyGroup( "sights", 1 );

		ViewModelArms = new AnimatedEntity( "models/first_person/first_person_arms.vmdl" );
		ViewModelArms.SetParent( ViewModelEntity, true );
		ViewModelArms.EnableViewmodelRendering = true;
	}

	/// <summary>
	/// We're done with the viewmodel - delete it
	/// </summary>
	public virtual void DestroyViewModel()
	{
		ViewModelEntity?.Delete();
		ViewModelEntity = null;
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
