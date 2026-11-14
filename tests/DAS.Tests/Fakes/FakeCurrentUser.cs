using DAS.Application.Abstractions;

namespace DAS.Tests.Fakes;

public sealed class FakeCurrentUser(string userName = TestData.UserName) : ICurrentUser
{
    public string UserName { get; } = userName;
}
