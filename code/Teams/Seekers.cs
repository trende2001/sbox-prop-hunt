using Sandbox;

namespace MyGame;

public class Seekers : BaseTeam
{
	public override string TeamName { get; } = "Seekers";
	public override Color TeamColor { get; } = Color.Green;

	public override void AddPlayer( Player player )
	{
		base.AddPlayer( player );

		player.Respawn();

		player.Inventory.AddItem( new Pistol() );
	}

	[ConCmd.Admin( "become_seeker" )]
	private static void BecomeSeeker()
	{
		if ( ConsoleSystem.Caller.Pawn is Player basePlayer )
		{
			basePlayer.Team?.RemovePlayer( basePlayer );
			Teams.Get<Seekers>().AddPlayer( basePlayer );
		}
	}
}
