using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace MyGame;

public abstract class BaseTeam
{
	public abstract string TeamName { get; }
	public abstract Color TeamColor { get; }

	public virtual float TeamPlayerPercentage { get; } = 0;
	
	public virtual int TeamPlayerMinimum { get; } = 0;
	
	public virtual int TeamPlayerMaximum { get; } = 65565;
	public IList<Player> Players { get; } = new List<Player>();

	public IEnumerable<Player> AlivePlayers
	{
		get
		{
			return Players.Where( p => p.LifeState == LifeState.Alive );
		}
	}
	
	public IEnumerable<Player> DeadPlayers
	{
		get
		{
			return Players.Where( p => p.LifeState == LifeState.Dead );
		}
	}
	
	public virtual IList<string> AdversaryTeams { get; } = new List<string>();

	protected BaseTeam()
	{
		Teams.RegisteredTeams.Add( this );
	}
	
	public virtual void DoGameruleTick()
	{
		if ( !PropHuntGame.PreventWin && ShouldWin() )
		{
			PropHuntGame.Current.OnTeamWin( this );
			return;
		}
	}
	
	public virtual void AddPlayer( Player player )
	{
		Players.Add( player );
		player.Team = this;
	}
	public virtual void RemovePlayer( Player player )
	{
		Players.Remove( player );
		player.Team = null;
	}
	public virtual void Reset()
	{
		foreach ( var player in Players )
		{
			player.Team = null;
		}

		Players.Clear();
	}

	public virtual bool ShouldWin()
	{
		foreach ( var team in this.AdversaryTeams )
		{
			if ( Teams.GetByName( team ).Players.Where( i => i.LifeState == LifeState.Alive ).Count() > 0 )
			{
				return false;
			}
		}

		return true;
	}
	
	/// <summary>
	/// Get a list of players that are in the specified team.
	/// </summary>
	/// <returns>Players in the specified team</returns>
	public IList<Player> GetPlayers()
	{
		if ( Game.IsServer )
		{
			return Players;
		}
		else
		{
			return Game.Clients.Where( i => i.Pawn is Player ply && ply.Team != null && ply.Team.TeamName == TeamName ).Select( i => i.Pawn as Player ).ToList();
		}
	}
}
