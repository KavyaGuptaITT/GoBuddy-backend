using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoBuddy.Domain.Entities
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; private set; }

        [Required]
        public int DriverId { get; private set; }

        [ForeignKey("DriverId")]
        public User Driver { get; private set; }

        [Required]
        [RegularExpression(@"^[A-Z]{2}\d{2}[A-Z]{1,2}\d{4}$", ErrorMessage = "Invalid vehicle number format")]
        public string VehicleNo { get; private set; }
        public string VehicleModel { get; private set; }

        [Required]
        public string LicenseNo { get; private set; }

        public int TotalSeats { get; private set; }

        public int AvailableSeats { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public byte[]? VehicleImage { get; private set; } 
        public byte[]? LicenseImage { get; private set; } 

        public Vehicle() { }

        public Vehicle(int driverId, string vehicleNo, string vehicleModel, string licenseNo, int totalSeats)
        {
            if (string.IsNullOrWhiteSpace(vehicleNo))
                throw new ArgumentException("Vehicle number cannot be empty");

            if (string.IsNullOrWhiteSpace(vehicleModel))
                throw new ArgumentException("Vehicle model cannot be empty");

            if (string.IsNullOrWhiteSpace(licenseNo))
                throw new ArgumentException("License number cannot be empty");

            if (totalSeats < 2 || totalSeats > 6)
                throw new ArgumentException("TotalSeats must be between 1 and 6");

            DriverId = driverId;
            VehicleNo = vehicleNo;
            VehicleModel = vehicleModel;
            LicenseNo = licenseNo;
            TotalSeats = totalSeats;
            AvailableSeats = totalSeats;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetImages(byte[]? vehicleImage, byte[]? licenseImage)
        {
            VehicleImage = vehicleImage ?? Array.Empty<byte>();
            LicenseImage = licenseImage ?? Array.Empty<byte>();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}