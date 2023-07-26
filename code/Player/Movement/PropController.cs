using System;
using Sandbox;

namespace MyGame;

public class PropController : WalkController
{
	public override void UpdateBBox( int forceduck = 0 )
	{
		Vector3 mins = Entity.CollisionBounds.Mins;
		Vector3 maxs = Entity.CollisionBounds.Maxs;

		float SmallestSize = Math.Max( Math.Min( maxs.x, maxs.y ), 2f );

		maxs.x = SmallestSize;
		maxs.y = SmallestSize;

		mins.x = maxs.x * -1;
		mins.y = maxs.y * -1;
		
		SetBBox( mins, maxs );
	}
}
