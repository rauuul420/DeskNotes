using DeskNotes.Core.Services;
using Xunit;

namespace DeskNotes.Infrastructure.Tests;

public sealed class DesktopLayerStatePolicyTests
{
    [Fact]
    public void ShouldShowDesktopBand_ReturnsTrueWhenDesktopIsForeground()
    {
        Assert.True(DesktopLayerStatePolicy.ShouldShowDesktopBand(
            desktopHostVisible: true,
            foregroundIsDesktop: true,
            foregroundIsManagedWidget: false,
            previousState: false));
    }

    [Fact]
    public void ShouldShowDesktopBand_ReturnsFalseWhenAnotherApplicationIsForeground()
    {
        Assert.False(DesktopLayerStatePolicy.ShouldShowDesktopBand(
            desktopHostVisible: true,
            foregroundIsDesktop: false,
            foregroundIsManagedWidget: false,
            previousState: true));
    }

    [Fact]
    public void ShouldShowDesktopBand_PreservesStateWhenWidgetReceivesUserFocus()
    {
        Assert.True(DesktopLayerStatePolicy.ShouldShowDesktopBand(
            desktopHostVisible: true,
            foregroundIsDesktop: false,
            foregroundIsManagedWidget: true,
            previousState: true));
    }

    [Fact]
    public void ShouldShowDesktopBand_DoesNotEnableBandWhenWidgetIsFocusedOutsideDesktop()
    {
        Assert.False(DesktopLayerStatePolicy.ShouldShowDesktopBand(
            desktopHostVisible: true,
            foregroundIsDesktop: false,
            foregroundIsManagedWidget: true,
            previousState: false));
    }

    [Fact]
    public void ShouldShowDesktopBand_ReturnsFalseWhenExplorerHostIsUnavailable()
    {
        Assert.False(DesktopLayerStatePolicy.ShouldShowDesktopBand(
            desktopHostVisible: false,
            foregroundIsDesktop: true,
            foregroundIsManagedWidget: false,
            previousState: true));
    }
}
