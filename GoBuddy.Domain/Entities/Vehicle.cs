using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GoBuddy.Domain.Entities;
namespace GoBuddy.Domain.Entities;

public class Vehicle
{
    [Key]
    public int PK_ID { get; private set; }
    public int FK_user_ID { get; private set; }

    [ForeignKey("FK_user_ID")]
    public User Driver { get; private set; } = null!;

    public string VehicleNo { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Vehicle(int driverId, string vehicleNo, int totalSeats)
    {

        if (string.IsNullOrWhiteSpace(vehicleNo))
            throw new ArgumentException("Vehicle number cannot be empty");

        if (totalSeats < 1 || totalSeats > 6)
            throw new ArgumentException("TotalSeats must be between 1 and 6");

        FK_user_ID = driverId;
        VehicleNo = vehicleNo;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}