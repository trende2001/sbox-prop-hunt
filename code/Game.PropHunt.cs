using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
		"ph_mp_preparingtime",
		Help = "The number of seconds to spend in the Preparing phase before the round starts. This is automatically multiplied by 1.5 on the first round of a map to allow all players to join. The default is 30 seconds.",
		Saved = true
	)]
	public static int PreparingRoundTime { get; set; } = 30;

	[ConVar.Replicated(
		"ph_mp_roundtime",
		Help = "The base number of seconds a round should go on, before any Haste Mode adjustments. The default is 360 seconds (6 minutes).",
		Saved = true
	)]
	public static int RoundTime { get; set; } = 5 * 60;

	[ConVar.Replicated(
		"ph_mp_postroundtime",
		Help = "The number of seconds to reserve for post-round time. The default is 15 seconds.",
		Saved = true
	)]
	public static int PostRoundTime { get; set; } = 15;
	
	[ConVar.Replicated(
		"ph_maxplayerstostart",
		Help = "Max amount of players to start the round.",
		Saved = true
	)]
	public static int MaxPlayersToStart { get; set; } = 2;
	
	[ConVar.Server(
		"ph_debug_preventwin",
		Help = "Debug: prevent all teams from winning, even if they meet their win conditions."
	)]
	public static bool PreventWin { get; set; } = false;
	
	[ConVar.Replicated( 
		"ph_enabledevcam",
		Help = "Enable devcam for developer purposes."
	)] 
	public static bool EnableDevCam { get; set; } = false;
	
	public List<long> RTVs { get; set; } = new();
	public string NextMap { get; set; } = null;

	[GameEvent.Tick.Server]
	public virtual void PreGameTick()
	{
		if ( RoundState == RoundState.None )
			RoundState = RoundState.WaitingForPlayers;
		if ( RoundState == RoundState.WaitingForPlayers && (Game.Clients.Count( i => i != null && i.IsValid && !((Player)i.Pawn).IsSpectator ) > MaxPlayersToStart ) )
			OnRoundPreparingPhase();
		if ( RoundState == RoundState.Preparing && TimeSinceRoundStateChanged > RoundLength ) 
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

	public virtual void OnRoundPreparingPhase()
	{
		RoundState = RoundState.Preparing;
		
		TimeSinceRoundStateChanged = 0;
		RoundLength = PreRoundTime;
	}

	[Net] private TimeUntil TimeUntilAnnounce { get; set; } = 4;
	
	public virtual void OnRoundStarting()
	{
		RoundState = RoundState.Starting;

		TimeSinceRoundStateChanged = 0;
		RoundLength = PreparingRoundTime;
		
		foreach ( var player in Entity.All.OfType<Player>().ToList() )
		{
			player.Health = 100;

			if ( player.LifeState != LifeState.Alive )
			{
				player.Respawn();
			}
		}
		
		AssignToTeams();

		Sound.FromScreen( To.Everyone, "seekers.unblind.vo" );
		
		PopupSystem.DisplayPopup( To.Everyone, "Hide or die", "The seekers will be unblinded in 30 seconds", 30f );
		
		AnnounceToTeam( "props.vo.beginmsg", "Props" );
		AnnounceToTeam( "seekers.vo.beginmsg", "Seekers" );
	}

	public virtual void OnRoundStart()
	{
		RoundState = RoundState.Started;

		TimeSinceRoundStateChanged = 0;
		RoundLength = RoundTime;
		
		Sound.FromScreen( To.Everyone, "seeker.unleashed.vo" );

		Decal.Clear( To.Everyone, true, true );
	}

	public virtual void AssignToTeams()
	{
		var spectator = Teams.Get<Spectator>();
		foreach ( var player in Entity.All.OfType<Player>().Where( x => x.IsSpectator ) )
		{
			Log.Info( "Adding " + player.Client.Name + " to spectator" );
			spectator.AddPlayer( player );
		}

		foreach ( var team in Teams.RegisteredTeams )
		{
			var plys = Entity.All.OfType<Player>().Where( x => !x.IsSpectator && x.LifeState == LifeState.Alive && x.Team == null ).OrderBy( x => Game.Random.Int( 0, 1000 ) ).ToList();
			var playerCount = Entity.All.OfType<Player>().Where( x => !x.IsSpectator && x.LifeState == LifeState.Alive ).Count();

			if ( team is Props || team is Spectator ) continue;
			
			Log.Info( $"Assigning to team {team.TeamName}" );

			var amountToAdd =
				(playerCount * team.TeamPlayerPercentage).FloorToInt()
				.Clamp( team.TeamPlayerMinimum, team.TeamPlayerMaximum ) - team.Players.Count();

			if ( amountToAdd == 0 )
			{
				var x = (playerCount * team.TeamPlayerPercentage).FloorToInt().Clamp( team.TeamPlayerMinimum, team.TeamPlayerMaximum );
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

	[ClientRpc]
	public static void AnnounceToTeam(string sound, string teamname)
	{
		// Loop through clients in a team, play a sound for each one of them
		Sound.FromScreen( To.Multiple( Teams.GetByName(teamname).Players.Select( x => x.Client ) ), sound);
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

		if ( team is Props )
			Sound.FromScreen(To.Everyone, "props.win.vo");
		else
		{
			Sound.FromScreen(To.Everyone, "seekers.win.vo");
		}
		
		//PopupSystem.DisplayPopup( "Game Over", $"{team.TeamName} win!" );
		OnRoundEnding();
	}

	public virtual void OnRoundEnding()
	{
		RoundState = RoundState.Ending;
		TimeSinceRoundStateChanged = 0;
		RoundLength = PostRoundTime;
		
		// TODO: show spectator panel to everyone (client rpc)
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
		ResetRound();
	}
	
	public static void Explosion( Entity weapon, Entity owner, Vector3 position, float radius, float damage, float forceScale )
	{
		// Effects
		Sound.FromWorld( "rust_pumpshotgun.shootdouble", position );
		Particles.Create( "particles/explosion/barrel_explosion/explosion_barrel.vpcf", position );

		// Damage, etc
		var overlaps = Entity.FindInSphere( position, radius );

		foreach ( var overlap in overlaps )
		{
			if ( overlap is not ModelEntity ent || !ent.IsValid() )
				continue;

			if ( ent.LifeState != LifeState.Alive )
				continue;

			if ( !ent.PhysicsBody.IsValid() )
				continue;

			if ( ent.IsWorld )
				continue;

			var targetPos = ent.PhysicsBody.MassCenter;

			var dist = Vector3.DistanceBetween( position, targetPos );
			if ( dist > radius )
				continue;

			var tr = Trace.Ray( position, targetPos )
				.Ignore( weapon )
				.WorldOnly()
				.Run();

			if ( tr.Fraction < 0.98f )
				continue;

			var distanceMul = 1.0f - Math.Clamp( dist / radius, 0.0f, 1.0f );
			var dmg = damage * distanceMul;
			var force = (forceScale * distanceMul) * ent.PhysicsBody.Mass;
			var forceDir = (targetPos - position).Normal;

			var damageInfo = DamageInfo.FromExplosion( position, forceDir * force, dmg )
				.WithWeapon( weapon )
				.WithAttacker( owner );

			ent.TakeDamage( damageInfo );

			
		}
	}
}
