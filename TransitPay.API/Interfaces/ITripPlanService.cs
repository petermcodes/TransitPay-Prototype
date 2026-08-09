using TransitPay.API.DTOs.TripPlan;
using TransitPay.API.Models;

namespace TransitPay.API.Interfaces;

public interface ITripPlanService
{
    Task<TripPlan> CreateTripPlanAsync(int userId, int cardId, int originTerminalId, int destinationTerminalId);
    Task<TripPlan?> GetActiveTripPlanAsync(int userId, int cardId);
    Task<TripPlan?> GetTripPlanByIdAsync(int planId, int userId, int cardId);
    Task<IEnumerable<TripPlan>> GetTripPlanHistoryAsync(int userId, int cardId);
    Task<TripPlan?> UpdateTripPlanDestinationAsync(int planId, int newDestinationTerminalId);
    Task<bool> CancelTripPlanAsync(int planId);
}
