using System;

namespace ConcurrentUpdates
{
	public abstract class AggregateRoot
	{
		protected AggregateRoot(DateTimeOffset lastModifiedOn)
		{
			LastModifiedOn = lastModifiedOn;
		}

		public DateTimeOffset LastModifiedOn { get; }
	}
}