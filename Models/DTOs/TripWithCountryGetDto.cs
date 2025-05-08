using System.ComponentModel.DataAnnotations;

namespace APBD_07.Models.DTOs;

public class TripWithCountryGetDto
{
    // NOTE: without get / set it getter returns empty brackets :O
    public required int IdTrip { get; set; }
    [MaxLength(120)] public required string Name { get; set; }
    [MaxLength(120)] public required string Description { get; set; }
    public required DateTime DateFrom { get; set; }
    public required DateTime DateTo { get; set; }
    public required int MaxPeople { get; set; }
    [MaxLength(120)] public required string CountryName { get; set; }
    public required int IdCountry { get; set; }
}