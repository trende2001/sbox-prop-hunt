using System.Linq;
using System.Net;
using Sandbox;

namespace MyGame;

public partial class PropHuntGame : GameManager {
	[Net] public TimeSince TimeSinceRoundStateChanged { get; set; } = 0;
	[Net] public RoundState RoundState { get; set; } = RoundState.None;
	[Net] public int RoundLength { get; set; } = 0;

	[ConVar.Replicated(
		"ph_round_count",
		Help = "The number of rounds before the map changes. Default is 8",
		Min = 1,
		Saved = true
	)]
	public static int RoundCount { get; set; } = 8;
	
	[ConVar.Replicated(
		"ph_mp_preroundtime",
		Help = "The number of seconds to spend in the Preparing phase before the round starts. This is automatically multiplied by 1.5 on the first round of a map to allow all players to join. The default is 30 seconds.",
		Saved = true
	)]
	public static int PreRoundTime { get; set; } = 30;

	[ConVar.Replicated(
		"ph_mp_roundtime",
		Help = "The base number of seconds a round should go on, before any Haste Mode adjustments. The default is 360 seconds (6 minutes).",
		Saved = true
	)]
	public static int RoundTime { get; set; } = 6 * 60;

	[ConVar.Replicated(
		"ph_mp_postroundtime",
		Help = "The number of seconds to reserve for post-round time. The default is 15 seconds.",
		Saved = true
	)]
	public static int PostRoundTime { get; set; } = 15;
	
	[ConVar.Server(
		"ph_debug_preventwin",
		Help = "Debug: prevent all teams from winning, even if they meet their win conditions."
	)]
	public static bool PreventWin { get; set; } = false;


	[GameEvent.Tick.Server]
	public virtual void PreGameTick()
	{
		if ( RoundState == RoundState.None )
			RoundState = RoundState.WaitingForPlayers;
		if ( RoundState == RoundState.WaitingForPlayers && (Game.Clients.Count > 1) )
			OnRoundStarting();
		if ( RoundState == RoundState.Starting && TimeSinceRoundStateChanged > RoundLength )
			OnRoundStart();
		if ( RoundState == RoundState.Started )
			RoundGameTick();
		if (RoundState == RoundState.Ending && TimeSinceRoundStateChanged > RoundLength )
			OnRoundEnd();

		/*
		if (RoundState == RoundState.WaitingForPlayers && Game.Clients.Count( i => i != null && i.IsValid && !((Player)i.Pawn).IsSpectator ) > 1)
			OnRoundStarting();
			*/

	}

	public virtual void OnRoundStarting()
	{
		RoundState = RoundState.Starting;

		TimeSinceRoundStateChanged = 0;
		RoundLength = PreRoundTime;

		Sound.FromScreen( To.Everyone, "round.countdown.30s" );
	}

	public virtual void OnRoundStart()
	{
		RoundState = RoundState.Started;

		TimeSinceRoundStateChanged = 0;
		RoundLength = RoundTime;
		
		AssignToTeams();
		
		Decal.Clear( To.Everyone, true, true );
	}

	public virtual void AssignToTeams()
	{
		var spectator = Teams.Get<Spectator>();

		foreach ( var team in Teams.RegisteredTeams )
		{
			var plys = Entity.All.OfType<Player>()
				.Where( x => x.LifeState == LifeState.Alive && x.Team == null )
				.OrderBy( x => Game.Random.Int( 0, 64 ) ).ToList();
			var playercount = All.OfType<Player>()
				.Count( x => x.LifeState == LifeState.Alive );

			if ( team is Props || team is Spectator ) continue;
			
			Log.Info( $"Assigning to team {team.TeamName}" );

			var amountToAdd =
				(playercount * team.TeamPlayerPercentage).FloorToInt()
				.Clamp( team.TeamPlayerMinimum, team.TeamPlayerMaximum ) - team.Players.Count();

			if ( amountToAdd == 0 )
			{
				var x = (playercount * team.TeamPlayerPercentage).FloorToInt().Clamp( team.TeamPlayerMinimum, team.TeamPlayerMaximum );
				Log.Info( $"Skipping assigning to team because we already have {team.Players.Count}/{x} players" );
				continue;
			}

			var toAdd = plys.Take( amountToAdd ).ToList();
			foreach ( Player player in toAdd )
			{
				Log.Info( $"Assigned {player.Client.Name}" );
				team.AddPlayer( player );
			}
		}
		
		var players = Entity.All.OfType<Player>().Where( x => x.Team == null ).OrderBy( x => Game.Random.Int( 0, 1000 ) ).ToList();

		// everything else is props

		foreach ( Player player in players )
		{
			Teams.Get<Props>().AddPlayer( player );
		}
	}

	public virtual void RoundGameTick()
	{
		foreach ( var team in Teams.RegisteredTeams )
		{
			team.DoGameruleTick();
		}

		if ( TimeSinceRoundStateChanged > RoundLength )
		{
			OnTeamWin( Teams.Get<Props>() );
		}

		return;
	}
	
	
	[Net] public string WinningTeamName { get; set; }
	[Net] public Color WinningTeamColor { get; set; }
	public BaseTeam WinningTeam { get; set; }
	public virtual void OnTeamWin( BaseTeam team )
	{
		WinningTeam = team;
		WinningTeamName = team.TeamName;
		WinningTeamColor = team.TeamColor;
		Log.Info( team.TeamName + " win!" );
		PopupSystem.DisplayPopup( "Game Over", $"{team.TeamName}s win!" );
		OnRoundEnding();
	}

	public virtual void OnRoundEnding()
	{
		RoundState = RoundState.Ending;
		TimeSinceRoundStateChanged = 0;
		RoundLength = PostRoundTime;
	}

	[Net] public int RoundNumber { get; set; } = 1;
	public virtual void OnRoundEnd()
	{
		RoundState = RoundState.Ended;
		TimeSinceRoundStateChanged = 0;
		Teams.Get<Props>().Reset();
		Teams.Get<Seekers>().Reset();
		
		// TODO: implement RTV and map votes

		RoundNumber++;
		CleanupRound();
	}
}
