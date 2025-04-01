using System;

namespace Burnout.Domaination;

class Snapshot<TEntityId>
    where TEntityId : struct
{
    public Guid Id { get; set; }
    public Guid? NextId { get; set; }
    public Guid CommitId { get; set; }
    public TEntityId EntityId { get; set; }
    public string Value { get; set; }
}
