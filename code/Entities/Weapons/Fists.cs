using Sandbox;

namespace MyGame;

public partial class Fists : Weapon
{
	public override Model ViewModel => Model.Load( "models/first_person/first_person_arms.vmdl" );

	public override float PrimaryAttackDelay => 0.45f;

	public override bool Automatic => true;

	public override bool ShowAmmoUI => false;
	//public override float SecondaryAttackDelay => 2.0f;

	public virtual void Attack( float damage = 0, float distance = 64, float spread = 0, float force = 0 )
	{
		Game.SetRandomSeed( Time.Tick );
		var forward = Owner.AimRay.Forward;
		forward += Vector3.Random * spread;
		var tr = Trace.Ray( Owner.AimRay.Position, Owner.AimRay.Position + (forward * distance) ).WithAnyTags( "solid", "prop", "propplayer" )
			.Ignore( Owner ).WithoutTags( "trigger" ).Run();
		if ( tr.Hit )
		{
			tr.Surface.DoBulletImpact( tr );
			if ( tr.Entity.IsValid() )
			{
				tr.Entity.TakeDamage( DamageInfo.FromBullet( tr.HitPosition, forward * force, damage )
					.WithWeapon( this ).WithAttacker( Owner ) );
				
				if ( !tr.Entity.Tags.Has( "propplayer" ) && tr.Entity.Tags.Has( "prop" ) )
				{
					Owner.TakeDamage( DamageInfo.Generic( 5f ) );
				}
				else if (tr.Entity.Tags.Has( "propplayer" ))
				{
					var blood = Particles.Create( "particles/impact.flesh.vpcf", tr.EndPosition );
					blood.SetPosition( 0, tr.EndPosition );
				}
			}
		}
	}

	public override bool CanReloadPrimary()
	{
		return false;
	}

	public override void CreateViewModel()
	{
		base.CreateViewModel();
		
		Game.AssertClient();

		if ( string.IsNullOrEmpty( ViewModel.ResourcePath ) )
			return;

		ViewModelEntity = new ViewModel()
		{
			Position = Position,
			Owner = Owner,
			EnableViewmodelRendering = true,
		};

		ViewModelEntity.SetModel( ViewModel.ResourcePath );
		ViewModelEntity.SetAnimGraph( "models/first_person/first_person_arms_punching.vanmgrph" );
	}

	public override void PrimaryAttack()
	{
		Attack( 25f, 100f );
		ViewModelEntity?.SetAnimParameter( "b_attack", true );
		
		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
	}

	public override void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.HoldType = CitizenAnimationHelper.HoldTypes.Punch;
		anim.Handedness = CitizenAnimationHelper.Hand.Both;
		anim.AimBodyWeight = 1.0f;
	}
}
