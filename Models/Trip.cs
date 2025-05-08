using System.ComponentModel.DataAnnotations;

namespace APBD_07.Models;

public class Trip
{
    public required int IdTrip;
    [MaxLength(120)] public required string Name;
    [MaxLength(120)] public required string Description;
    public required DateTime DateFrom;
    public required DateTime DateTo;
    public required int MaxPeople;
}