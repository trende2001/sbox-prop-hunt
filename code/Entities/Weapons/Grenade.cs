using Sandbox;
using System;

namespace MyGame;
public class Grenade : Prop
{
	public override void Spawn()
	{
		base.Spawn();

		Model = Cloud.Model("https://asset.party/mapperskai/weapon_f1");
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		
		Tags.Add( "grenade" );
	}

	public override void Touch( Entity other )
	{
		base.Touch( other );
		
		PropHuntGame.Explosion( this, Owner, this.Position, 200f, 120f, 100f );
		Delete();
	}
}
