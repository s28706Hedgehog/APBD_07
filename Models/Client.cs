using System.ComponentModel.DataAnnotations;

namespace APBD_07.Models;

public class Client
{
    public required int Id { get; set; }
    [MaxLength(120)] public required string FirstName { get; set; }
    [MaxLength(120)] public required string LastName { get; set; }
    [MaxLength(120)] public required string Email { get; set; }
    [MaxLength(120)] public required string Telephone { get; set; }
    [MaxLength(120)] public required string Pesel { get; set; }
}