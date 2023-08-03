namespace MyGame;

using Sandbox;
using System;
using System.Collections.Generic;

public class Props : BaseTeam
{
	public override string TeamName { get; } = "Props";
	public override Color TeamColor { get; } = Color.FromBytes( 51, 153, 255 );
	
	public static IList<string> advTeams = new List<string> { "Seekers" };

	public override IList<string> AdversaryTeams { get; } = advTeams;


	public override void AddPlayer( Player player )
	{
		base.AddPlayer( player );
		
		player.Tags.Add( "propplayer" );
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
