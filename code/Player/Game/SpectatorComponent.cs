namespace MyGame;

using Sandbox;
using Sandbox.UI;
using System;
using System.Linq;

public partial class SpectatorComponent : SimulatedComponent, IDisposable
{
	[Net, Predicted] public Player CurrentlySpectating { get; set; }
	[Net, Predicted] public SpectatingMode SpectateMode { get; set; } = SpectatingMode.FreeRoam;
	private Panel panel;

	public override void Simulate( IClient cl )
	{
		if ( panel == null && Game.IsClient && Entity == Game.LocalPawn && ! HUDRootPanel.Current.Children.Where(i => i is SpectatorPlayerPanel).Any() ) panel = new SpectatorPlayerPanel();

		if ( Input.Pressed( "Attack2" ) )
		{
			SpectateMode += 1;
			if ( SpectateMode > (SpectatingMode)1 )
				SpectateMode = 0;
		}

		if ( Input.Pressed( "Use" ) )
		{
			var tra = Trace.Ray( Entity.AimRay, 300 )
				.Ignore( Entity );
			if ( SpectateMode == SpectatingMode.SpectatePlayer )
			{
				tra = tra.Ignore( CurrentlySpectating );
			}
			var tr = tra.Run();
			if ( tr.Hit && tr.Entity is Player ply )
			{
				SpectateMode = SpectatingMode.SpectatePlayer;
				CurrentlySpectating = ply;
			}
		}

		if ( SpectateMode == SpectatingMode.SpectatePlayer )
		{
			DoSpectatePlayer();
		}
		if ( SpectateMode == SpectatingMode.FreeRoam )
		{
			DoFreeRoam();
		}
	}
	Rotation RotSpecOverride;
	bool FirstPerson = true;
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );
		DoViewmodelStuff();
		if ( SpectateMode == SpectatingMode.SpectatePlayer )
		{
			Camera.FirstPersonViewer = null;
			if ( CurrentlySpectating != null )
			{
				if ( FirstPerson )
				{

					Camera.Position = CurrentlySpectating.AimRay.Position;
					RotSpecOverride = Rotation.Lerp( RotSpecOverride, CurrentlySpectating.NetworkedEyeRotation, Time.Delta * 32f ).Angles().WithRoll( 0 ).ToRotation();
					Camera.Rotation = RotSpecOverride;
					Camera.FirstPersonViewer = CurrentlySpectating;
				}
				else if ( !FirstPerson )
				{

					Vector3 targetPos;
					var center = CurrentlySpectating.AimRay.Position;

					var pos = center;
					var rot = Entity.ViewAngles.ToRotation();

					float distance = 130.0f * Entity.Scale;
					targetPos = pos;
					targetPos += rot.Forward * -distance;

					var tr = Trace.Ray( pos, targetPos )
						.WithAnyTags( "solid" )
						.WithoutTags( "player", "trigger" )
						.Run();

					Camera.Position = tr.EndPosition;
					Camera.Rotation = Entity.ViewAngles.ToRotation();
					Camera.FirstPersonViewer = null;
				}
				/*
				if ( CurrentlySpectating.Inventory != null )
				{ 
					Entity.Inventory.ActiveChildInput = CurrentlySpectating.Inventory.ActiveChild;
				}
				*/
			}
		}
	}
	public void DoSpectatePlayer()
	{
		if ( !Entity.Components.TryGet<MovementComponent>( out var b1 ) && Entity.Components.RemoveAny<MovementComponent>() )
		{
			Entity.Components.Add( new MovementComponent() );
		}
		if ( !Entity.Components.TryGet<CameraComponent>( out var b2 ) && Entity.Components.RemoveAny<CameraComponent>() )
		{
			Entity.Components.Add( new CameraComponent() );
		}
		var players = Sandbox.Entity.All.OfType<Player>().Where( x => x.LifeState == LifeState.Alive ).OrderBy( x => x.Client.Name ).ToList();
		if ( CurrentlySpectating == null )
		{
			CurrentlySpectating = players.FirstOrDefault();
		}
		if ( Input.Pressed( "Attack1" ) || CurrentlySpectating?.LifeState != LifeState.Alive )
		{
			var index = players.IndexOf( CurrentlySpectating );
			CurrentlySpectating = players.ElementAtOrDefault( index + 1 );
			if ( CurrentlySpectating == null )
			{
				CurrentlySpectating = players.FirstOrDefault();

				if ( CurrentlySpectating == null )
				{
					DoFreeRoam();
				}
			}
		}

		if ( CurrentlySpectating != null )
		{
			Entity.Position = CurrentlySpectating.Position;
			Entity.Velocity = CurrentlySpectating.Velocity;
			Entity.Health = CurrentlySpectating.Health;

			if ( Input.Pressed( "Reload" ) )
			{
				FirstPerson = !FirstPerson;
			}

			//Entity.TeamName = CurrentlySpectating.TeamName;
			//Entity.TeamColour = CurrentlySpectating.TeamColour;

			/*
			if ( CurrentlySpectating.Inventory != null )
			{
				Entity.Inventory.Items = CurrentlySpectating.Inventory.Items;
				Entity.Ammo.Ammo = CurrentlySpectating.Ammo.Ammo;
				Entity.Inventory.ActiveChild = CurrentlySpectating.Inventory.ActiveChild;
			}

			*/
		}
	}
	public void DoFreeRoam()
	{

		if ( !Entity.Components.TryGet<NoclipController>( out var b1 ) && Entity.Components.RemoveAny<MovementComponent>() )
		{
			Entity.Components.Add( new NoclipController() );
		}
		if ( !Entity.Components.TryGet<FirstPersonCamera>( out var b2 ) && Entity.Components.RemoveAny<CameraComponent>() )
		{
			Entity.Components.Add( new FirstPersonCamera() );
		}
		Entity.Health = 0;
		/*
		Entity.Inventory.Items.Clear();
		Entity.Inventory.ActiveChild = null;
		Entity.Inventory.ActiveChildInput = null;

		*/
	}
	public Entity ActiveChild { get; set; }
	Entity PreviousActiveChild { get; set; }
	public void DoViewmodelStuff()
	{
		if ( Game.IsServer ) return;
		if ( SpectateMode == SpectatingMode.SpectatePlayer && CurrentlySpectating != null && CurrentlySpectating.Inventory != null && CurrentlySpectating.Inventory.ActiveChild != null )
		{
			ActiveChild = CurrentlySpectating.Inventory.ActiveChild;
		}
		else
		{
			ActiveChild = null;
		}

		// Check to see if we've changed weapons
		if ( ActiveChild != PreviousActiveChild )
		{
			if ( PreviousActiveChild is Carriable cr1 ) cr1.OnActiveEnd();
			PreviousActiveChild = ActiveChild;
			if ( ActiveChild is Carriable cr2 ) cr2.OnActiveStart();
		}
	}

	public void Dispose()
	{
		if ( Game.IsClient ) panel?.Delete();
	}
}
public enum SpectatingMode
{
	FreeRoam,
	SpectatePlayer,
	PossessProp
}
