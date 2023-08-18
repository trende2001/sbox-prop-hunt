using System;
using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyGame;

public partial class MapVoteEntity : Entity
{
	public static MapVoteEntity Current;
	MapVotePanel Panel;

	[Net, Predicted]
	public IDictionary<IClient, string> Votes { get; set; }

	[Net]
	public string WinningMap { get; set; } = "baks.office";

	[Net]
	public RealTimeUntil VoteTimeLeft { get; set; } = 100;

	[Net] bool ShouldMakeMapVotePanel { get; set; } = false;
	bool HasMadeMapVote = false;
	public string LocalClientVote = "";

	async Task GetMaps()
	{
		var take = 0;
		var b = new List<Package>();
		for ( var i = 0; i < 5; i++ )
		{
			b = b.Union( (await Package.FindAsync( "type:map game:trend.prop_hunt", 200, take )).Packages ).ToList();
			take += 200;
		}
		

		var f = b.OrderBy( x => Game.Random.Int( 0, 100000 ) ).Take( 20 ).ToList();
		foreach ( var pkg in f )
		{
			var d = new NetworkablePackage();
			if ( pkg == null ) continue;
			d.Title = pkg.Title;
			d.FullIdent = pkg.FullIdent;
			d.Thumb = pkg.Thumb;


			d.OrgTitle = pkg.Org.Title;
			d.OrgIdent = pkg.Org.Ident;
			d.OrgThumb = pkg.Org.Thumb;
			Maps.Add( d );
		}
		//WinningMap = Maps.First().FullIdent;
		VoteTimeLeft = 30;
		ShouldMakeMapVotePanel = true;
		//ShowVotePanelOnClients();
	}
	[Net] public IList<NetworkablePackage> Maps { get; set; }
	public override void Spawn()
	{
		base.Spawn();
		_ = GetMaps();
		Transmit = TransmitType.Always;
		Current = this;
	}

	[GameEvent.Tick.Client]
	void ClTick()
	{
		if ( ShouldMakeMapVotePanel && !HasMadeMapVote )
		{
			Panel = new MapVotePanel();
			Panel.OwnerEntity = this;
			HUDRootPanel.Current.AddChild( Panel );
			HasMadeMapVote = true;
		}
	}
	[ClientRpc]
	public void ShowVotePanelOnClients()
	{
		Panel = new MapVotePanel();
		Panel.OwnerEntity = this;
		HUDRootPanel.Current.AddChild( Panel );
	}
	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Current = this;
	}
	protected override void OnDestroy()
	{
		base.OnDestroy();

		Panel?.Delete();
		Panel = null;

		if ( Current == this )
			Current = null;
	}

	[GameEvent.Client.Frame]
	public void OnFrame()
	{
		if ( Panel != null )
		{
			if ( Input.Pressed( "View" ) )
			{
				Panel.Visible = !Panel.Visible;
			}

			var seconds = VoteTimeLeft.Relative.FloorToInt().Clamp( 0, 100000000 );

			Panel.TimeText = Format( TimeSpan.FromSeconds( seconds ) );
		}
	}
	[GameEvent.Tick.Server]
	public void Tick()
	{
		if ( VoteTimeLeft < 0 )
		{
			UpdateWinningMap();
			Game.ChangeLevel( WinningMap );
		}
	}
	void CullInvalidClients()
	{
		/*foreach ( var entry in Votes.Keys.Where( x => !x.IsValid() ).ToArray() )
		{
			Votes.Remove( entry );
		}*/
	}

	void UpdateWinningMap()
	{
		if ( Votes.Count == 0 )
			return;

		WinningMap = Votes.GroupBy( x => x.Value ).OrderBy( x => x.Count() ).Last().Key;
	}

	void SetVote( IClient client, string map )
	{
		CullInvalidClients();
		Votes[client] = map;

		UpdateWinningMap();
		RefreshUI();
	}

	[ClientRpc]
	void RefreshUI()
	{
		if ( Panel == null ) return;
		Panel.VoteUpdater++;
		//Panel.UpdateFromVotes( Votes );
	}

	[ConCmd.Server( "ph_setvote" )]
	public static void SetVote( string map )
	{
		if ( Current == null || ConsoleSystem.Caller == null )
			return;

		Current.SetVote( ConsoleSystem.Caller, map );
	}
	public static void PredictedSetVote( string map )
	{
		if ( Game.IsClient )
		{
			SetVote( map );
			if ( Current == null )
				return;
			Current.LocalClientVote = map;
		}
	}

	public static string Format( TimeSpan timespan )
	{
		var format = timespan.Hours >= 1 ? @"h\:mm\:ss" : @"m\:ss";
		return timespan.ToString( format );
		//var format = timespan.Minutes >= 1 ? @"m\:ss" : @"ss";
		//return timespan.ToString(format);
	}
}

public partial class NetworkablePackage : BaseNetworkable
{
	[Net] public string Title { get; set; }
	[Net] public string FullIdent { get; set; }
	[Net] public string Thumb { get; set; }
	[Net] public string OrgTitle { get; set; }
	[Net] public string OrgIdent { get; set; }
	[Net] public string OrgThumb { get; set; }
}
