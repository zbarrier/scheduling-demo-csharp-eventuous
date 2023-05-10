using Eventuous;

namespace DoctorDay.Application.Tests;
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
