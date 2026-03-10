namespace GoBuddy.Application.Interfaces
{
    public interface IRideCancellationService
    {
        Task CancelRideByPassengerAsync(int rideRequestId);
        Task CancelRideByDriverAsync(int driverId);
    }
}