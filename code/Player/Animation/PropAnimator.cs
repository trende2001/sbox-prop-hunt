using Sandbox;
using MyGame;

namespace MyGame;

public class PropAnimator : AnimationComponent
{
	public override void Simulate( IClient cl )
	{
		
		var ply = Entity as Player;

		if ( ply.LockRotation )
			ply.Rotation = ply.ViewAngles.WithPitch( 0f ).ToRotation();


		Rotation idealRotation = ply.EyeLocalRotation;
		idealRotation.x = 0f;
		idealRotation.y = 0f;
		ply.Rotation = Rotation.Slerp(ply.Rotation, idealRotation, Time.Delta * 25f);
	}
}
