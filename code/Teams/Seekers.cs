using Sandbox;
using System.Collections.Generic;

namespace MyGame;

public class Seekers : BaseTeam
{
	public override string TeamName { get; } = "Seekers";
	public override Color TeamColor { get; } = Color.Green;
	
	public static IList<string> advTeams = new List<string> { "Props" };

	public override IList<string> AdversaryTeams { get; } = advTeams;

	public override float TeamPlayerPercentage => 1f / PlayersPerSeeker;

	public override int TeamPlayerMinimum { get; } = 1;
	public static float PlayersPerSeeker { get; set; } = 2f;

	public override void AddPlayer( Player player )
	{
		base.AddPlayer( player );
		
		player.Respawn();
		
		player.Inventory.AddItem( new MP5() );
		player.Inventory.AddItem( new Pistol() );
		player.Inventory.AddItem( new PropInteractor() );

		player.Ammo.GiveAmmo( AmmoType.Pistol, 55);
		player.Ammo.GiveAmmo( AmmoType.SMG, 100 );
		player.Ammo.GiveAmmo( AmmoType.Buckshot, 1 );
		
		player.Tags.Add( "seeker" );
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
