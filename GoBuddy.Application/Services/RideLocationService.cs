using GoBuddy.Application.Interfaces;
using GoBuddy.Domain.Entities;

namespace GoBuddy.BusinessLayer.Services
{
    public class LocationService : IRideLocationService
    {
        private readonly IRideSessionRepository _rideSessionRepository;

        public LocationService(IRideSessionRepository rideSessionRepository)
        {
            _rideSessionRepository = rideSessionRepository;
        }

        public async Task UpdateLocationAsync(int driverId, double latitude, double longitude)
        {
            var rideSession = await _rideSessionRepository.GetByDriverIdAsync(driverId);

            if (rideSession == null)
                throw new ApplicationException("Active ride session not found for driver");

            rideSession.UpdateLocation(latitude, longitude);

            await _rideSessionRepository.SaveChangesAsync();
        }

        public async Task<List<RideSession>> GetNearbyDriversAsync(double latitude, double longitude, double radiusInKm)
        {
            var activeSessions = await _rideSessionRepository.GetActiveSessionsAsync();

            List<RideSession> nearbyDrivers = new List<RideSession>();

            foreach (var rideSession in activeSessions)
            {
                double distance = CalculateDistance(latitude, longitude, rideSession.CurrentLatitude, rideSession.CurrentLongitude);

                if (distance <= radiusInKm)
                    nearbyDrivers.Add(rideSession);
            }

            return nearbyDrivers;
        }

        private double CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2)
        {

             double earthRadius = 6371;
             double dLatitude = ToRadians(latitude2 - latitude1);
             double dLongitude = ToRadians(longitude2 - longitude1);

             double a =
              Math.Sin(dLatitude / 2) * Math.Sin(dLatitude / 2) +
              Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) *
              Math.Sin(dLongitude / 2) * Math.Sin(dLongitude / 2);

             double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private double ToRadians(double angle)
        {
            return angle * Math.PI / 180;
        }
    }
}