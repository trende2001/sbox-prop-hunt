using Sandbox;

namespace MyGame;

public class MP5 : Gun
{
	public override Model ViewModel => Cloud.Model( "https://asset.party/facepunch/v_mp5" );
	public override Model WorldModel => Cloud.Model( "https://asset.party/facepunch/w_mp5" );
	public override float PrimaryAttackDelay => 0.09f;
	public override float SecondaryAttackDelay => 100f;

	public override float SecondaryReloadDelay => 0.1f;
	public override float PrimaryReloadDelay => 3.0f;
	public override int MaxPrimaryAmmo => 25;

	public override AmmoType PrimaryAmmoType => AmmoType.SMG;
	public override AmmoType SecondaryAmmoType => AmmoType.Buckshot;
	

	public override bool Automatic { get; } = true;
	

	public override void PrimaryAttack()
	{
		PrimaryAmmo -= 1;
		ShootBullet( 12, 0.09f );
		PlaySound( "mp5_fire" );
		(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "b_attack", true );
		
		if ( Game.IsClient )
		{
			ShootEffects();
			DoViewPunch( 1f );
			new ScreenShake.Perlin( 0.5f, 4.0f, 1.0f, 0.5f );
			ScreenShake.Apply();
		}
	}

	public override void SecondaryAttack()
	{
		SecondaryAmmo -= 1;


		var aim = Owner.AimRay;
		
		TimeSinceSecondaryAttack = 1.25f;

		if ( Game.IsServer )
		{
			using ( Prediction.Off() )
			{
				var grenade = new Grenade
				{
					Position = aim.Position + aim.Forward * 3.0f,
					Owner = Owner,
				};

				grenade.PhysicsBody.Velocity = aim.Forward * 600.0f + Owner.Rotation.Up * 200.0f + Owner.Velocity;
			}
		}
	}
	
	public override void ReloadPrimary()
	{
		base.ReloadPrimary();
		PlaySound( "mp5_reload" );
		ViewModelEntity?.SetAnimParameter( "b_reload", true );
	}
}
