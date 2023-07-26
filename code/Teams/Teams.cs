using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGame;
public class Teams
{
	public static List<BaseTeam> RegisteredTeams = new();

	public static T Get<T>() where T : BaseTeam
	{
		if ( RegisteredTeams.OfType<T>().Any() )
		{
			return RegisteredTeams.OfType<T>().First();
		}
		return TypeLibrary.GetType<T>().Create<T>();
	}

	public static BaseTeam GetByName( string name )
	{
		if ( RegisteredTeams.OfType<BaseTeam>().Any( x => x.TeamName == name ) )
		{
			return RegisteredTeams.OfType<BaseTeam>().First( x => x.TeamName == name );
		}
		return null;
	}

	public static void InitialiseTeams()
	{
		var teamTypes = TypeLibrary.GetTypes<BaseTeam>()
			.Where( type => type.TargetType.IsSubclassOf( typeof( BaseTeam ) ) ).ToList();
		if ( teamTypes.Count() == 0 )
		{
			Log.Error( "No team classes were found." );
			throw new Exception( "No team classes were found." );
		}

		foreach ( var team in teamTypes )
		{
			if ( RegisteredTeams.Any( x => x.GetType() == team.TargetType ) )
				continue;
			team.Create<BaseTeam>();
		}
	}

}
