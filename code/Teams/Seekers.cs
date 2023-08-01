using Sandbox;

namespace MyGame;

public class Seekers : BaseTeam
{
	public override string TeamName { get; } = "Seekers";
	public override Color TeamColor { get; } = Color.Green;

	public override float TeamPlayerPercentage => 1f / PlayersPerSeeker;

	public override int TeamPlayerMinimum { get; } = 1;
	public static float PlayersPerSeeker { get; set; } = 2f;

	public override void AddPlayer( Player player )
	{
		base.AddPlayer( player );

		player.Respawn();

		player.Inventory.AddItem( new Pistol() );
	}

	public override bool ShouldWin()
	{
		return base.ShouldWin();
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
