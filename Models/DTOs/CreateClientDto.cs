using System.ComponentModel.DataAnnotations;

namespace APBD_07.Models.DTOs;

public class CreateClientDto
{
    [MaxLength(120)] public required string FirstName { get; set; }
    [MaxLength(120)] public required string LastName { get; set; }
    [MaxLength(120)] public required string Email { get; set; }
    [MaxLength(120)] public required string Telephone { get; set; }
    [MaxLength(120)] public required string Pesel { get; set; }
}