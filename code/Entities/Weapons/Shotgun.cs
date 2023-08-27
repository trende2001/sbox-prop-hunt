using Sandbox;

namespace MyGame;

public partial class Shotgun : Gun
{
	public override Model ViewModel => Model.Load( "weapons/rust_pumpshotgun/v_rust_pumpshotgun.vmdl" );
	public override Model WorldModel => Model.Load( "weapons/rust_pumpshotgun/rust_pumpshotgun.vmdl" );
	public override float PrimaryAttackDelay => 1;
	public override float SecondaryAttackDelay => 1;
	public override float PrimaryReloadDelay => 0.5f;
	public override int MaxPrimaryAmmo => 10;
	
	public override bool SequentialReloading => true;

	public override AmmoType PrimaryAmmoType => AmmoType.Buckshot;

	public override void PrimaryAttack()
	{
		PrimaryAmmo -= 1;
		ShootBullet( 10, 0.09f, 40, 8 );
		PlaySound( "rust_pumpshotgun.shoot" );
		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );

		if ( Game.IsClient )
		{
			ShootEffects();
			DoViewPunch( 7f );
		}
	}
	
	public override void ReloadPrimary()
	{
		TimeSincePrimaryAttack = -0.5f;
		base.ReloadPrimary();
		ViewModelEntity?.SetAnimParameter( "reload", true );
	}

	public override void SimulateAnimator( CitizenAnimationHelper anim )
	{
		base.SimulateAnimator( anim );
		anim.HoldType = CitizenAnimationHelper.HoldTypes.Shotgun;
		anim.AimBodyWeight = 1.0f;
	}
}
