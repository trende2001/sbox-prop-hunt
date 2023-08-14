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
	public static PropHuntGame Current { get; protected set; }

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
			Chat.AddChatEntry( To.Everyone,null, $"{client.Name} has joined the game", isInfo: true);
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );
		
		if ( reason != NetworkDisconnectionReason.SERVER_SHUTDOWN && reason != NetworkDisconnectionReason.Kicked )
			Chat.AddChatEntry( To.Everyone,null, $"{cl.Name} has left the game", isInfo: true);
	}
	
	[ClientRpc]
	public override void OnKilledMessage( long leftid, string left, long rightid, string right, string method )
	{
		KillFeed.Current?.AddEntry( leftid, left, rightid, right, method );
	}
}
