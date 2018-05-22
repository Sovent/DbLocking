using System;
using System.Data;
using System.Linq;
using Dapper;

namespace ConcurrentUpdates
{
	public class TeamRepository : ITeamsRepository
	{
		public TeamRepository(IDbConnection dbConnection)
		{
			_dbConnection = dbConnection;
		}

		public Team GetTeam(Guid teamId)
		{
			var queryResult = _dbConnection.Query(
				"SELECT Teams.TeamId, ParticipantId, TeamMaxSize FROM Teams LEFT JOIN Participants ON Teams.TeamId=Participants.TeamId WHERE Teams.TeamId=@teamId", 
				new {teamId});
			if (queryResult == null || !queryResult.Any())
			{
				return null;
			}

			var first = queryResult.First();
			if (first.ParticipantId == null)
			{
				return new Team(first.TeamId, first.TeamMaxSize, Enumerable.Empty<Guid>());
			}

			var participants = queryResult.Select(row => (Guid) row.ParticipantId);
			return new Team(first.TeamId, first.TeamMaxSize, participants);
		}

		public void SaveTeam(Team team)
		{
			using (var transaction = _dbConnection.BeginTransaction())
			{
				foreach (var teamParticipant in team.Participants)
				{
					_dbConnection.Execute(
						"IF NOT EXISTS (SELECT * FROM Participants WHERE ParticipantId=@participantId) INSERT INTO Participants VALUES (@participantId, @teamId)",
						new {participantId = teamParticipant, teamId = team.Id},
						transaction);
				}
				transaction.Commit();
			}
		}

		private readonly IDbConnection _dbConnection;
	}
}