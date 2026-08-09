using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TransitPay.API.Data;
using TransitPay.API.Enums;
using TransitPay.API.Models;
using TransitPay.API.Services;
using Xunit;

namespace TransitPay.API.Tests;

public class TripServiceTests
{
    private static TransitPayDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TransitPayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TransitPayDbContext(options);
    }

    private static TripService CreateService(TransitPayDbContext context)
    {
        return new TripService(context, NullLogger<TripService>.Instance);
    }

    private static void SeedTerminals(TransitPayDbContext context)
    {
        context.Terminals.Add(new Terminal { TerminalId = 1, TerminalName = "Central", IsActive = true });
        context.Terminals.Add(new Terminal { TerminalId = 2, TerminalName = "Harbor", IsActive = true });
        context.Terminals.Add(new Terminal { TerminalId = 3, TerminalName = "Airport", IsActive = true });
    }

    [Fact]
    public async Task StartTrip_CreatesActiveTrip_WithBoardingOriginInitialized()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var trip = await service.StartTripAsync(driverId: 5, originTerminalId: 1, finalDestinationTerminalId: 3);

        Assert.NotNull(trip);
        Assert.Equal(TripStatus.Active, trip.TripStatus);
        Assert.NotNull(trip.StartedAt);
        Assert.Equal(1, trip.CurrentBoardingOriginTerminalId);
        Assert.Equal(1, trip.OriginTerminalId);
        Assert.Equal(3, trip.FinalDestinationTerminalId);
        Assert.Equal("Central → Airport", trip.RouteName);
    }

    [Fact]
    public async Task StartTrip_RejectsWhenDriverHasActiveTrip()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        context.Trips.Add(new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 2,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Harbor",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartTripAsync(driverId: 5, originTerminalId: 1, finalDestinationTerminalId: 3));

        Assert.Contains("already have an active trip", ex.Message);
    }

    [Fact]
    public async Task StartTrip_RejectsInvalidTerminals()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartTripAsync(driverId: 5, originTerminalId: 1, finalDestinationTerminalId: 99));

        Assert.Equal("Invalid origin or destination terminal.", ex.Message);
    }

    [Fact]
    public async Task UpdateCurrentBoardingOrigin_UpdatesOriginAndTimestamp()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var updated = await service.UpdateCurrentBoardingOriginAsync(trip.TripId, newOriginTerminalId: 2);

        Assert.Equal(2, updated.CurrentBoardingOriginTerminalId);
        Assert.NotNull(updated.BoardingOriginUpdatedAt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCurrentBoardingOrigin_RejectsNonActiveTrip()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Completed,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateCurrentBoardingOriginAsync(trip.TripId, newOriginTerminalId: 2));

        Assert.Contains("Only active trips can be updated", ex.Message);
    }

    [Fact]
    public async Task UpdateCurrentBoardingOrigin_RejectsUnknownTerminal()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateCurrentBoardingOriginAsync(trip.TripId, newOriginTerminalId: 99));

        Assert.Equal("Invalid or inactive origin terminal.", ex.Message);
    }

    [Fact]
    public async Task UpdateCurrentBoardingOrigin_RejectsInactiveTerminal()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        context.Terminals.Add(new Terminal { TerminalId = 4, TerminalName = "Closed", IsActive = false });
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateCurrentBoardingOriginAsync(trip.TripId, newOriginTerminalId: 4));

        Assert.Equal("Invalid or inactive origin terminal.", ex.Message);
    }

    [Fact]
    public async Task EndTrip_OnlyAllowsActiveTrip()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ended = await service.EndTripAsync(trip.TripId);

        Assert.Equal(TripStatus.Completed, ended.TripStatus);
        Assert.NotNull(ended.EndedAt);
    }

    [Fact]
    public async Task EndTrip_RejectsNonActiveTrip()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EndTripAsync(trip.TripId));

        Assert.Contains("Only active trips can be ended", ex.Message);
    }

    [Fact]
    public async Task CancelTrip_AllowsActiveOrPending()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var cancelled = await service.CancelTripAsync(trip.TripId);

        Assert.Equal(TripStatus.Cancelled, cancelled.TripStatus);
        Assert.NotNull(cancelled.EndedAt);
    }

    [Fact]
    public async Task CancelTrip_RejectsCompletedTrip()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 1,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Completed,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelTripAsync(trip.TripId));

        Assert.Equal("Cannot cancel a completed trip.", ex.Message);
    }

    [Fact]
    public async Task GetActiveTrip_IncludesBoardingOriginTerminal()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        var trip = new Trip
        {
            DriverId = 5,
            OriginTerminalId = 1,
            FinalDestinationTerminalId = 3,
            CurrentBoardingOriginTerminalId = 2,
            RouteName = "Central → Airport",
            TripStatus = TripStatus.Active,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Trips.Add(trip);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var active = await service.GetActiveTripAsync(driverId: 5);

        Assert.NotNull(active);
        Assert.Equal(TripStatus.Active, active!.TripStatus);
        Assert.Equal(2, active.CurrentBoardingOriginTerminalId);
        Assert.NotNull(active.CurrentBoardingOriginTerminal);
        Assert.Equal("Harbor", active.CurrentBoardingOriginTerminal!.TerminalName);
    }

    [Fact]
    public async Task GetActiveTrip_ReturnsNull_WhenNoActiveTrip()
    {
        using var context = CreateContext();
        SeedTerminals(context);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var active = await service.GetActiveTripAsync(driverId: 5);

        Assert.Null(active);
    }
}