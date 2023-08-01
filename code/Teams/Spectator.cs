using System.Linq;
using Sandbox;

namespace MyGame;

public class Spectator : BaseTeam
{
	public override string TeamName { get; } = "Spectators";
	public override Color TeamColor { get; } = Color.FromBytes(166, 166, 166);

	public override float TeamPlayerPercentage { get; } = 0f;

	public override void AddPlayer( Player player )
	{
		base.AddPlayer( player );

		if ( player.LifeState == LifeState.Alive )
		{
			player.TakeDamage( (new DamageInfo { Damage = player.Health * 99 }).WithTag( "suicide" ) );
		}
	}

	public override void RemovePlayer( Player player )
	{
		base.RemovePlayer( player );

		if ( PropHuntGame.Current.RoundState != RoundState.Started )
		{
			player.Respawn();
		}

		foreach ( var child in player.Children.Where( i => i.Tags.Has( "clothes" ) ) )
		{
			child.EnableDrawing = true;
		}
	}

	public override bool ShouldWin()
	{
		return false;
	}
}
