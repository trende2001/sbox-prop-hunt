using System;
using Sandbox;

namespace MyGame;

public class ThirdPersonCamera : CameraComponent
{
	public override void FrameSimulate( IClient cl )
	{

		var pl = Entity as Player;
		// Update rotation every frame, to keep things smooth  

		pl.EyeRotation = pl.ViewAngles.ToRotation();

		Vector3 targetPos;

		var pos = pl.Position + (Vector3.Up * Math.Max(pl.CollisionBounds.Maxs.z * pl.Scale * 0.75f, 8f));
		var rot = Rotation.FromAxis( Vector3.Up, 4 ) * pl.EyeRotation;

		float distance = 50.0f * pl.Scale;
		targetPos = pos + rot.Backward * distance;

		var tr = Trace.Ray( pos, targetPos )
			.WithAnyTags( "solid" )
			.Ignore( pl )
			.Radius( 8 )
			.Run();

		// Make our camera see-through instead
		Camera.Position = tr.EndPosition;
		Camera.Rotation = pl.ViewAngles.ToRotation();

		// Set field of view to whatever the user chose in options
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		Camera.ZNear = 1f;

		// Set the first person viewer to null, this isn't first person
		Camera.FirstPersonViewer = null;
	}
	public override void BuildInput()
	{
		if ( Game.LocalClient.Components.TryGet<DevCamera>( out var _ ) )
			return;

		var pl = Entity as Player;
		var viewAngles = (pl.ViewAngles + Input.AnalogLook).Normal;
		pl.ViewAngles = viewAngles.WithPitch( viewAngles.pitch.Clamp( -89f, 89f ) );
		return;
	}
}
