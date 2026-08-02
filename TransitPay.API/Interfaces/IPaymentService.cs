namespace TransitPay.API.Interfaces;

public interface IPaymentService
{
    Task<object> ProcessPaymentAsync(int cardId, int stationId, decimal amount);
}
