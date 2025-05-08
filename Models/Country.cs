using System.ComponentModel.DataAnnotations;

namespace APBD_07.Models;

public class Country
{
    public required int Id { get; set; }
    [MaxLength(120)]
    public required string Name { get; set; }
}