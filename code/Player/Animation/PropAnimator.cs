using Sandbox;
using MyGame;

namespace MyGame;

public class PropAnimator : AnimationComponent
{
	public override void Simulate( IClient cl )
	{
		
		var ply = Entity as Player;
		// where should we be rotated to
		var turnSpeed = 0.02f;

		if ( ply.LockRotation )
		{
			ply.Rotation = ply.ViewAngles.WithPitch( 0f ).ToRotation();
			return;
		}


		Rotation idealRotation = ply.EyeLocalRotation;
		idealRotation.x = 0f;
		idealRotation.y = 0f;
		ply.Rotation = Rotation.Slerp(ply.Rotation, idealRotation, Time.Delta * 25f);
	}
}
