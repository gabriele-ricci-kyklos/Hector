using System;

namespace Hector.Data.Queries
{
    public record SqlParameter(Type Type, string Name, object Value);
}
