namespace GoBuddy.API.SharedConstants
{
    public static class SignalRConstants
    {
        public const string DriverOnline = "DriverOnline";
        public const string DriverOffline = "DriverOffline";
        public const string LocationUpdated = "LocationUpdated";
        public const string IncomingRideRequest = "IncomingRideRequest";

        public const string RequestSent = "RequestSent";
        public const string RequestFailed = "RequestFailed";
        public const string RequestTimeout = "RequestTimeout";
        public const string RequestExpired = "RequestExpired";

        public const string RideAccepted = "RideAccepted";
        public const string RideRejected = "RideRejected";
        public const string RideConfirmed = "RideConfirmed";
        public const string RideCancelled = "RideCancelled";
        public const string RideCompleted = "RideCompleted";

        public const string DriverArrived = "DriverArrived";

        public const string PinConfirmed = "PinConfirmed";
        public const string PinError = "PinError";

        public const string CancelError = "CancelError";
        public const string CannotGoOffline = "CannotGoOffline";
        public const string Error = "Error";
    }
}