using APBD_07.Models;
using APBD_07.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace APBD_07.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IConfiguration config) : ControllerBase
{
    /**
     * Returns list containing data of both Trip and Client_Trip table for Client specified by @id
     */
    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetTripsWithCountries(int id)
    {
        var result = new List<TripWithClientTripGetDto>();

        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString); // automatically closes connection when done

        const string sqlQuery = """
                                    SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.PaymentDate, ct.RegisteredAt FROM Client_Trip ct
                                    INNER JOIN Trip t ON t.IdTrip = ct.IdTrip
                                    WHERE ct.IdClient = @id;
                                """;

        await using var command = new SqlCommand(sqlQuery, connection);
        command.Parameters.AddWithValue("@id", id);

        await connection.OpenAsync();

        await using var reader = await command.ExecuteReaderAsync(); // you also need to be closed automatically

        while (await reader.ReadAsync())
        {
            result.Add(new TripWithClientTripGetDto()
                {
                    IdTrip = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    MaxPeople = reader.GetInt32(5),
                    PaymentDate = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    RegisteredAt = reader.GetInt32(7)
                }
            );
        }

        if (result.Count == 0)
        {
            // From what I remember we will handle it in different layer
            throw new NotFoundException(
                "Either client with provided Id does not exist or there are no trips registered");
            // Probably I should check which one but well...
        }

        return Ok(result);
    }

    /**
     * Creates new Client in DataBase based on data got with Post request
     */
    [HttpPost]
    public async Task<IActionResult> CreateClient(CreateClientDto client)
    {
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString); // automatically closes connection when done

        var sqlPost = """
                        insert into Client(FirstName, LastName, Email, Telephone, Pesel)
                        values (@FirstName, @LastName, @Email, @Telephone, @Pesel);
                        SELECT scope_identity();
                      """;
        await using var command = new SqlCommand(sqlPost, connection);

        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);

        await connection.OpenAsync();
        var result = await command.ExecuteScalarAsync();
        int newId = Convert.ToInt32(result);

        // https://stackoverflow.com/questions/61982609/returning-a-post-response-without-providing-an-location-uri
        return Created($"/clients/{newId}", newId);
    }

    /**
     * Register Client for specified Trip ( if met requiremenets )
     */
    [HttpPut("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> RegisterClientOnTrip(int clientId, int tripId)
    {
        return null;
    }
}