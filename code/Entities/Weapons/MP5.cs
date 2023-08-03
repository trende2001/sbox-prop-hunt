using Sandbox;

namespace MyGame;

public class MP5 : Gun
{
	public override Model ViewModel => Cloud.Model( "https://asset.party/facepunch/v_mp5" );
	public override Model WorldModel => Cloud.Model( "https://asset.party/facepunch/w_mp5" );
	public override float PrimaryAttackDelay => 0.08f;
	public override float PrimaryReloadDelay => 3.0f;
	public override int MaxPrimaryAmmo => 25;
	public override AmmoType PrimaryAmmoType => AmmoType.Pistol;

	public override bool Automatic { get; } = true;
	
	private ParticleSystem EjectBrass;

	public override void OnActiveStart()
	{
		base.OnActiveStart();
		
		EjectBrass = Cloud.ParticleSystem( "https://asset.party/facepunch/9mm_ejectbrass" );
	}

	public override void PrimaryAttack()
	{
		PrimaryAmmo -= 1;
		ShootBullet( 10, 0.02f );
		PlaySound( "mp5_fire" );
		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "b_attack", true );
		if ( Game.IsClient )
		{
			ShootEffects();
			DoViewPunch( 1f );
		}
	}
	
	public override void ShootEffects()
	{
		base.ShootEffects();

		Particles.Create( EjectBrass.ResourcePath, EffectEntity, "eject" );
	}
	public override void ReloadPrimary()
	{
		base.ReloadPrimary();
		PlaySound( "mp5_reload" );
		ViewModelEntity?.SetAnimParameter( "b_reload", true );
	}
}
