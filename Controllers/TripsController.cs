using APBD_07.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_07.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController(IConfiguration config) : ControllerBase
{
    /**
     * Return's list of all the Trips with their Country names
     */
    [HttpGet]
    public async Task<IActionResult> GetTripsWithCountries()
    {
        var result = new List<TripWithCountryGetDto>();

        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString); // automatically closes connection when done

        // const string sqlQuery = "SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip";
        const string sqlQuery = """
                                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name, c.IdCountry
                                FROM Trip t INNER JOIN Country_Trip ct ON ct.IdTrip = t.IdTrip
                                INNER JOIN Country c ON c.IdCountry = ct.IdCountry;
                                """;

        await using var command = new SqlCommand(sqlQuery, connection);
        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync(); // you also need to be closed automatically

        while (await reader.ReadAsync())
        {
            result.Add(new TripWithCountryGetDto()
                {
                    // I guess no need to validate if null cause data comes from database which feels like safe-space :D
                    IdTrip = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    CountryName = reader.GetString(6),
                    IdCountry = reader.GetInt32(7)
                }
            );
        }

        return Ok(result);
    }
}