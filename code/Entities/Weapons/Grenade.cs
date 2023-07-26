using Sandbox;
namespace MyGame;
public class Grenade : Throwable
{
	public override Model ViewModel => Cloud.Model( "https://asset.party/facepunch/v_usp" );
	public override Model WorldModel => Cloud.Model( "https://asset.party/facepunch/w_usp" );
	public override void Throw()
	{
		if ( Game.IsServer )
		{
			var Nade = new ModelEntity();
			Nade.Model = WorldModel;
			Nade.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
			Nade.Position = Owner.AimRay.Position + (Owner.AimRay.Forward * 40);
			Nade.PhysicsBody.Velocity = (Owner.AimRay.Forward * 500) + (Owner.AimRay.Forward.EulerAngles.ToRotation().Up * 200);
		}
	}
}
