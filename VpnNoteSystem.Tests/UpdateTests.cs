using Moq;
using VpnNoteSystem.App.Services;

namespace VpnNoteSystem.Tests;

public class UpdateTests
{
    [Fact]
    public async Task CheckForUpdates_NoNetwork_ReturnsFalse()
    {
        var authMock = new Mock<IAuthService>();
        var securityLogMock = new Mock<ISecurityLogService>();
        var updateService = new UpdateService(authMock.Object, securityLogMock.Object);

        var result = await updateService.CheckForUpdatesAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ApplyUpdate_WithoutAuth_ThrowsException()
    {
        var authMock = new Mock<IAuthService>();
        var securityLogMock = new Mock<ISecurityLogService>();
        var updateService = new UpdateService(authMock.Object, securityLogMock.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => updateService.ApplyUpdateAsync());
    }

    [Fact]
    public async Task ApplyUpdate_NoUpdateAvailable_ReturnsMessage()
    {
        var user = new VpnNoteSystem.App.Models.User
        {
            Id = 1,
            Username = "admin",
            Role = "admin",
            IsActive = true
        };

        var authMock = new Mock<IAuthService>();
        authMock.Setup(a => a.GetCurrentUser()).Returns(user);
        authMock.Setup(a => a.IsAuthenticated).Returns(true);

        var securityLogMock = new Mock<ISecurityLogService>();
        var updateService = new UpdateService(authMock.Object, securityLogMock.Object);

        var result = await updateService.ApplyUpdateAsync();

        Assert.Contains("Нет доступных обновлений", result);
        Assert.Contains("[INFO]", result);
    }

    [Fact]
    public async Task CheckForUpdates_NoNetwork_DoesNotLog()
    {
        var authMock = new Mock<IAuthService>();
        var securityLogMock = new Mock<ISecurityLogService>();
        var updateService = new UpdateService(authMock.Object, securityLogMock.Object);

        await updateService.CheckForUpdatesAsync();

        securityLogMock.Verify(s => s.LogAsync(
            null, "SYSTEM", "UPDATE_CHECK",
            It.IsAny<string>(), true), Times.Never);
    }
}
