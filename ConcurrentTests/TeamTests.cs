using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ConcurrentUpdates;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcurrentTests
{
    [TestClass]
    public class TeamTests
    {
		[TestInitialize]
	    public void Initialize()
		{
			var connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=""C:\Users\Sovent\Documents\Visual Studio 2017\Projects\ConcurrentUpdates\groups.mdf"";Integrated Security=True;Connect Timeout=30";
			_sqlConnection = new SqlConnection(connectionString);
			_sqlConnection.Open();
			_sqlConnection.Execute("DROP TABLE Teams;" +
			                       "DROP TABLE Participants;" +
			                       "CREATE TABLE Teams(TeamId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, TeamMaxSize INT);" +
			                      "CREATE TABLE Participants(ParticipantId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, TeamId UNIQUEIDENTIFIER)"); 
			var repository = new TeamRepository(connectionString);
			_teamsService = new TeamsService(repository);
		}

		[TestCleanup]
	    public void Teardown()
	    {
			_sqlConnection.Close();   
	    }

        [TestMethod]
        public void AddParticipantOverTheLimit_ParticipantNotAdded()
        {
	        var team = InitializeTeam();
			_teamsService.AddParticipant(team, Guid.NewGuid());
			_teamsService.AddParticipant(team, Guid.NewGuid());
	        var loadedTeam = _teamsService.GetTeam(team);
			Assert.IsTrue(loadedTeam.Participants.Count() <= loadedTeam.TeamMaxSize);
        }

		[TestMethod]
	    public void ConcurrentlyAddParticipantOverTheLimit_ParticipantNotAdded()
	    {
		    var team = InitializeTeam();
			Parallel.Invoke(
				() => _teamsService.AddParticipant(team, Guid.NewGuid()), 
				() => _teamsService.AddParticipant(team, Guid.NewGuid()));
		    var loadedTeam = _teamsService.GetTeam(team);
			Assert.IsTrue(loadedTeam.Participants.Count() <= loadedTeam.TeamMaxSize);
	    }

	    private Guid InitializeTeam()
	    {
		    var teamId = Guid.NewGuid();
		    using (var transaction = _sqlConnection.BeginTransaction())
		    {
			    _sqlConnection.Execute(
					"INSERT INTO Teams VALUES (@teamId, @teamMaxSize)", 
					new {teamId, teamMaxSize = 2},
					transaction);
			    _sqlConnection.Execute(
					"INSERT INTO Participants VALUES (@participantId, @teamId)",
				    new {teamId, participantId = Guid.NewGuid()},
					transaction);
				transaction.Commit();
			}

		    return teamId;
	    }

	    private TeamsService _teamsService;
	    private IDbConnection _sqlConnection;
    }
}
