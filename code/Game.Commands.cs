using Sandbox;
using System.Linq;

namespace MyGame;
public partial class PropHuntGame
{


	[ConCmd.Server( "ent_create" )]
	public static void SpawnEntity( string entName )
	{
		Log.Info( "creating " + entName );
		var owner = ConsoleSystem.Caller.Pawn as Player;

		if ( owner == null )
		{
			Log.Info( "Failed to create " + entName );
			return;
		}

		var entityType = TypeLibrary.GetType<Entity>( entName )?.TargetType;
		if ( entityType == null )
		{
			Log.Info( "Failed to create " + entName );
			return;
		}

		var tr = Trace.Ray( owner.AimRay, 500 )
			.UseHitboxes()
			.Ignore( owner )
			.Size( 2 )
			.Run();

		var ent = TypeLibrary.Create<Entity>( entityType );

		ent.Position = tr.EndPosition;
		ent.Rotation = Rotation.From( new Angles( 0, owner.AimRay.Forward.EulerAngles.yaw, 0 ) );

		//Log.Info( $"ent: {ent}" );
	}

	[ConCmd.Server( "give" )]
	public static void GiveWeapon( string entName )
	{
		if ( ConsoleSystem.Caller.Pawn is Player player )
		{
			if ( player.Inventory.AddItem( TypeLibrary.Create<Entity>( entName ) ) )
			{
				Log.Info( $"Giving {entName} to {player.Client.Name}" );
				return;
			}
			Log.Info( $"Failed to give {entName}. Not a valid entity." );
		}
	}
	
	public static void CleanUpFull()
	{ 
		// Tell our game that all clients have just left.
		foreach ( IClient cl in Game.Clients )
		{
			PropHuntGame.Current.ClientDisconnect( cl, NetworkDisconnectionReason.SERVER_SHUTDOWN );
		}

		// Cleanup on clients
		CleanupClientEntities( To.Everyone );
		
		// Reset the map, this will respawn all map entities
		Game.ResetMap( Entity.All.Where( x => x is Player ).ToArray() );

		// delete the game.
		PropHuntGame.Current.Delete();

		// Create a brand new game
		PropHuntGame.Current = new PropHuntGame();

		// Fake a post level load after respawning entities, just incase something uses it
		PropHuntGame.Current.PostLevelLoaded();

		// Tell our new game that all clients have just joined to set them all back up.
		foreach ( IClient cl in Game.Clients )
		{
			PropHuntGame.Current.ClientJoined( cl );
		}
	} 

	public static void CleanUpRound()
	{

		// Tell our game that all clients have just left.
		foreach ( IClient cl in Game.Clients )
		{
			PropHuntGame.Current.ClientDisconnect( cl, NetworkDisconnectionReason.SERVER_SHUTDOWN );
		}

		
		Entity[] keep = { HUDEntity.Current, PropHuntGame.Current };

		// Cleanup on clients
		CleanupClientEntities( To.Everyone );

		// Reset the map, this will respawn all map entities
		Game.ResetMap( keep );

		(PropHuntGame.Current as PropHuntGame).RoundState = RoundState.None;
		(PropHuntGame.Current as PropHuntGame).TimeSinceRoundStateChanged = 0;
		

		HUDEntity.Current.Delete();
		_ = new HUDEntity();


		// Fake a post level load after respawning entities, just incase something uses it
		//MyGame.Current.PostLevelLoaded();

		// Tell our new game that all clients have just joined to set them all back up.
		foreach ( IClient cl in Game.Clients )
		{
			PropHuntGame.Current.ClientJoined( cl );
		}
	}
	
	[ConCmd.Admin("reset_round")]
	public static void ResetRound()
	{
		CleanUpRound();
	}
	
	[ConCmd.Admin( "start_game" )]
	public static void StartGameCommand()
	{
		PropHuntGame.Current.RoundState = RoundState.Starting;
		PropHuntGame.Current.TimeSinceRoundStateChanged = 0;
		PropHuntGame.Current.RoundLength = 0;
	}  

	static bool DefaultCleanupFilter( Entity ent )
	{
		// Basic Source engine stuff
		var className = ent.ClassName;
		if ( className == "player" || className == "worldent" || className == "worldspawn" || className == "soundent" || className == "player_manager" )
		{
			return false;
		}

		// When creating entities we only have classNames to work with..
		// The filtered entities below are created through code at runtime, so we don't want to be deleting them
		if ( ent == null || !ent.IsValid ) return true;

		// Gamemode entity
		if ( ent is BaseGameManager ) return false;

		// HUD entities
		if ( ent.GetType().IsBasedOnGenericType( typeof( HudEntity<> ) ) ) return false;

		// Player related stuff, clothing and weapons
		foreach ( var cl in Game.Clients )
		{
			if ( ent.Root == cl.Pawn ) return false;
		}

		// Do not delete view model
		if ( ent is BaseViewModel ) return false;
		
		return true;
	}
	[ClientRpc]
	public static void CleanupClientHUD()
	{

		HUDRootPanel.Current.Delete( true );
	}

	[ClientRpc]
	public static void CleanupClientEntities()
	{ 
		Decal.Clear( true, true );
		foreach ( Entity ent in Entity.All )
		{
			if ( ent.IsClientOnly )
				ent.Delete();
		}
	}

}
