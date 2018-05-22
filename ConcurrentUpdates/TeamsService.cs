using System;

namespace ConcurrentUpdates
{
	public class TeamsService
	{
		public TeamsService(ITeamsRepository teamsRepository)
		{
			_teamsRepository = teamsRepository;
		}

		public Team GetTeam(Guid id)
		{
			return _teamsRepository.GetTeam(id);
		}

		public void AddParticipant(Guid teamId, Guid participantId)
		{
			var team = _teamsRepository.GetTeam(teamId);
			team.AddParticipant(participantId);
			_teamsRepository.SaveTeam(team);
		}

		private readonly ITeamsRepository _teamsRepository;
	}
}