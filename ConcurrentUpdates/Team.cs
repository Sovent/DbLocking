using System;
using System.Collections.Generic;

namespace ConcurrentUpdates
{
    public class Team : AggregateRoot
	{
		public Team(Guid id, int teamMaxSize, IEnumerable<Guid> participants, DateTimeOffset lastModifiedOn) : base(lastModifiedOn)
		{
			Id = id;
			TeamMaxSize = teamMaxSize;
			_participants = new HashSet<Guid>(participants);
		}

		public Guid Id { get; }

		public int TeamMaxSize { get; }

		public IEnumerable<Guid> Participants => _participants;

		public void AddParticipant(Guid participantId)
		{
			if (_participants.Count >= TeamMaxSize)
			{
				return;
			}

			_participants.Add(participantId);
		}

		private readonly HashSet<Guid> _participants;
	}
}
