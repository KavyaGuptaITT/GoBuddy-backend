namespace GoBuddy.API.SharedConstants

{

    public static class MessageConstants
    {
        public const string CannotGoOffline = "Complete all active rides before going offline.";

        public const string PendingOrActiveRequestExists = "You already have a pending or active request.";

        public const string DriverUnavailable = "Driver is no longer available.";

        public const string NoSeatsAvailable = "No seats available with this driver.";

        public const string DriverBusyPickingUp = "Driver is busy picking up another passenger. Try again shortly.";

        public const string DriverTooFar = "You are too far from the driver (max 3 km).";

        public const string DestinationTooFar = "Your destination is too far from the driver's current route.";

        public const string RequestSent = "Request sent! Waiting for driver response...";

        public const string DriverDidNotRespond = "Driver did not respond in time.";

        public const string RequestExpired = "A ride request has expired.";

        public const string NoPendingRequests = "No pending requests.";

        public const string DriverDeclinedRequest = "Driver declined your request.";

        public const string RideNotFound = "Ride not found.";

        public const string PinAlreadyConfirmed = "PIN already confirmed.";

        public const string PinConfirmedEnjoy = "PIN confirmed! Enjoy your ride.";

        public const string PinConfirmedBoarded = "PIN confirmed! Passenger boarded.";

        public const string DriverArrivedPickup = "Your driver has arrived at your pickup!";

        public const string ArrivedAskPin = "Arrived at pickup. Ask passenger for PIN.";

        public const string CannotCancelAfterPin = "Cannot cancel after PIN is confirmed.";

        public const string DriverDisconnected = "Driver disconnected. Ride cancelled.";

        public const string PassengerDisconnected = "Passenger disconnected.";
        public const string DriverWentOffline = "Driver went offline.";

    }

    public static class RoleConstants
    {
        public const string System = "System";

        public const string SystemTooManyPins = "System (Too many wrong PIN attempts)";

        public const string Driver = "Driver";

        public const string Passenger = "Passenger";

    }

}