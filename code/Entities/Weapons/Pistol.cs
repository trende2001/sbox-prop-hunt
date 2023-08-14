using Sandbox;

namespace MyGame;
public class Pistol : Gun
{
	public override Model ViewModel => Cloud.Model( "https://asset.party/facepunch/v_usp" );
	public override Model WorldModel => Cloud.Model( "https://asset.party/facepunch/w_usp" );
	public override float PrimaryAttackDelay => 0.1f;
	public override float PrimaryReloadDelay => 3.0f;
	public override int MaxPrimaryAmmo => 17;
	public override AmmoType PrimaryAmmoType => AmmoType.Pistol;

	public override void OnActive()
	{
		base.OnActive();
		
		ViewModelEntity?.SetAnimParameter( "b_deploy", true );
	}

	public override void PrimaryAttack()
	{
		PrimaryAmmo -= 1;
		ShootBullet( 20, 0.02f );
		PlaySound( "rust_pistol.shoot" );
		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "b_attack", true );
		if ( Game.IsClient )
		{
			ShootEffects();
			DoViewPunch( 1f );
		}
	}
	public override void ReloadPrimary()
	{
		base.ReloadPrimary();
		ViewModelEntity?.SetAnimParameter( "b_reload", true );
	}
}
