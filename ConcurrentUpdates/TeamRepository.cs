using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;

namespace ConcurrentUpdates
{
	public class TeamRepository : ITeamsRepository
	{
		public TeamRepository(string connectionString)
		{
			_connectionString = connectionString;
		}

		public Team GetTeam(Guid teamId)
		{
			using (var dbConnection = new SqlConnection(_connectionString))
			{
				var queryResult = dbConnection.Query(
					"SELECT Teams.TeamId, ParticipantId, TeamMaxSize, LastModifiedOn FROM Teams LEFT JOIN Participants ON Teams.TeamId=Participants.TeamId WHERE Teams.TeamId=@teamId",
					new {teamId});
				if (queryResult == null || !queryResult.Any())
				{
					return null;
				}

				var first = queryResult.First();
				if (first.ParticipantId == null)
				{
					return new Team(first.TeamId, first.TeamMaxSize, Enumerable.Empty<Guid>(), default(DateTimeOffset));
				}

				var participants = queryResult.Select(row => (Guid) row.ParticipantId);
				return new Team(first.TeamId, first.TeamMaxSize, participants, first.LastModifiedOn);
			}
		}

		public void SaveTeam(Team team)
		{
			using (var dbConnection = new SqlConnection(_connectionString))
			{
				dbConnection.Open();
				using (var transaction = dbConnection.BeginTransaction())
				{
					var rowsUpdated = dbConnection.Execute(
						"UPDATE Teams SET LastModifiedOn=SYSDATETIMEOFFSET() WHERE TeamId=@id AND LastModifiedOn=@lastModifiedOn",
						new { id = team.Id, lastModifiedOn = team.LastModifiedOn },
						transaction);
					foreach (var teamParticipant in team.Participants)
					{
						dbConnection.Execute(
							"IF NOT EXISTS (SELECT * FROM Participants WHERE ParticipantId=@participantId) INSERT INTO Participants VALUES (@participantId, @teamId)",
							new {participantId = teamParticipant, teamId = team.Id},
							transaction);
					}
					if (rowsUpdated == 0)
					{
						transaction.Rollback();
						throw new DBConcurrencyException("Optimistic lock failed");
					}

					transaction.Commit();
				}
				dbConnection.Close();
			}
		}

		private readonly string _connectionString;
	}
}