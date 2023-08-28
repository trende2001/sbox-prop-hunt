using Sandbox;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace MyGame;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class PropHuntGame : GameManager
{
	public new static PropHuntGame Current { get; protected set; }

	public PropHuntGame()
	{
		if ( Game.IsServer )
		{
			_ = new HUDEntity();
		}
		
		Teams.InitialiseTeams();

		Current = this;
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		Event.Run( "Player.Connect", client, this );

		base.ClientJoined( client );

		// Create a player for this client to play with
		var pawn = new Player();
		client.Pawn = pawn;

		if ( pawn.LifeState == LifeState.Alive && PropHuntGame.Current.RoundState < RoundState.Started &&
		     pawn.Team is not Spectator )
		{
			pawn.UpdateClothes( client );
			pawn.Clothing.DressEntity( pawn );
		}

		if ( RoundState != RoundState.None )
			Chat.AddChatEntry( To.Everyone, "", $"{client.Name} has joined the game", isInfo: true);
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		Event.Run( "Player.Disconnect", cl, reason );
		if ( cl.Pawn is Player ply )
		{
			ply.Team?.RemovePlayer( ply );
		}
		
		base.ClientDisconnect( cl, reason );

		if ( reason != NetworkDisconnectionReason.SERVER_SHUTDOWN && reason != NetworkDisconnectionReason.Kicked )
			Chat.AddChatEntry( To.Everyone, "", $"{cl.Name} has left the game", isInfo: true);
	}

	public override void DoPlayerDevCam( IClient client )
	{
		Game.AssertServer();

		var camera = client.Components.Get<DevCamera>( false );

		if ( camera == null && EnableDevCam )
		{
			camera = new DevCamera();
			client.Components.Add( camera );
			return;
		}

		if ( EnableDevCam )
		{
			camera.Enabled = !camera.Enabled;
		}
	}

}
