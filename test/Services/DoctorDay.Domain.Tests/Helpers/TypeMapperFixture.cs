using Eventuous;

namespace DoctorDay.Domain.Tests;
public class TypeMapperFixture : IDisposable
{
    public TypeMapperFixture()
    {
        TypeMap.RegisterKnownEventTypes();
    }

    public void Dispose()
    {
    }
}
