using Microsoft.AspNetCore.SignalR;
using GoBuddy.Application.Interfaces;

public class LocationHub : Hub
{
    private readonly IRideLocationService _locationService;

    public LocationHub(IRideLocationService locationService)
    {
        _locationService = locationService;
    }

    public async Task SendLocation(double latitude, double longitude)
    {
        //string userIdValue = Context.User?.FindFirst("UserId")?.Value ?? "0";
        //int driverId = int.Parse(userIdValue);

        int driverId = 11;

        await _locationService.UpdateLocationAsync(driverId, latitude, longitude);

        await Clients.All.SendAsync("ReceiveDriverLocation", latitude, longitude);
    }
}