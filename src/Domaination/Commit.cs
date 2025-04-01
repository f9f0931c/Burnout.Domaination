using System;

namespace Burnout.Domaination;

class Commit<StreamId>
    where StreamId : struct
{
    public StreamId EntityId { get; set; }
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public IDomainEvent[] Events { get; set; }
}

class StoredEvent 
{
    public long CommitId { get; set; }
    public int Ordinal { get; set; }
    public Guid TypeId { get; set; }
    public string Serialized { get; set; }
}
