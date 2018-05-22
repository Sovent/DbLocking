using System;

namespace ConcurrentUpdates
{
	public interface ITeamsRepository
	{
		Team GetTeam(Guid teamId);
		void SaveTeam(Team team);
	}
}