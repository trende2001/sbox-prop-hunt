namespace MyGame;

using Sandbox;

public class Props : BaseTeam
{
	public override string TeamName { get; } = "Props";
	public override Color TeamColor { get; } = Color.FromBytes( 51, 153, 255 );

	public override void AddPlayer( Player player )
	{
		base.AddPlayer( player );

		player.Respawn();
	}

	[ConCmd.Admin( "become_props" )]
	private static void BecomeProps()
	{
		if ( ConsoleSystem.Caller.Pawn is Player basePlayer )
		{
			basePlayer.Team?.RemovePlayer( basePlayer );
			Teams.Get<Props>().AddPlayer( basePlayer );
		}
	}
}
