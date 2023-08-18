using Sandbox;
using System;

namespace MyGame;
public class Grenade : BasePhysics
{
	public static readonly Model WorldModel = Cloud.Model( "https://asset.party/mapperskai/weapon_f1" );

	public override void Spawn()
	{
		base.Spawn();
		
		Model = WorldModel;
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		
		Tags.Add( "grenade" );
	}

	[GameEvent.Tick.Server]
	public void Simulate()
	{
		var trace = Trace.Ray( Position, Position )
			.Size( 24 )
			.Ignore( this )
			.Ignore( Owner )
			.Run();

		Position = trace.EndPosition;
		
		if (trace.Hit)
			BlowUp();
	}

	public void BlowUp()
	{
		PropHuntGame.Explosion( this, Owner, this.Position, 200f, 320f, 60f );
		Delete();
	}
}
