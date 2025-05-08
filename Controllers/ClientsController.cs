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
            // Probably I should check which one but well...

            // throw new NotFoundException(
            //     "Either client with provided Id does not exist or there are no trips registered");

            return NotFound("Either client with provided Id does not exist or there are no trips registered");
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
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString); // automatically closes connection when done

        // Checks if client with specified id exists in database
        var sqlClientCheck = "SELECT COUNT(*) FROM Client WHERE Client.IdClient = @clientId;";
        await using var clientCheckCommand = new SqlCommand(sqlClientCheck, connection);
        clientCheckCommand.Parameters.AddWithValue("@clientId", clientId);

        await connection.OpenAsync();
        var clientCheckResult = await clientCheckCommand.ExecuteScalarAsync();

        if (Convert.ToInt32(clientCheckResult) == 0)
        {
            // throw new NotFoundException($"Client with that Id does not exist [ClientId:{clientId}]");
            return NotFound($"Client with that Id does not exist [ClientId:{clientId}]");
        }

        // Checks if trip with specified id exists in database
        var sqlTripCheck = "SELECT COUNT(*) FROM Trip WHERE Trip.IdTrip = @tripId; ";
        await using var tripCheckCommand = new SqlCommand(sqlTripCheck, connection);
        tripCheckCommand.Parameters.AddWithValue("@tripId", tripId);

        var tripCheckResult = await tripCheckCommand.ExecuteScalarAsync();

        if (Convert.ToInt32(tripCheckResult) == 0)
        {
            // throw new NotFoundException($"Trip with that Id does not exist [TripId:{tripId}]");
            return NotFound($"Trip with that Id does not exist [TripId:{tripId}]");
        }

        // Another chcek, Yeeeeeeeeeeeeeeeeey, otherwise it throws Exception for existing keys
        var sqlAlreadyRegistered = """
                                       SELECT COUNT(*) FROM Client_Trip 
                                       WHERE IdClient = @clientId AND IdTrip = @tripId;
                                   """;
        await using var alreadyRegistCommand = new SqlCommand(sqlAlreadyRegistered, connection);
        alreadyRegistCommand.Parameters.AddWithValue("@clientId", clientId);
        alreadyRegistCommand.Parameters.AddWithValue("@tripId", tripId);
        var alreadyResult = await alreadyRegistCommand.ExecuteScalarAsync();
        var isAlready = Convert.ToInt32(alreadyResult) > 0;
        if (isAlready)
        {
            // throw new InvalidOperationException("Client is already registered for this trip -_-");
            return Conflict("Client is already registered for this trip -_-");
        }

        // Checks if limit of clients for this trip has been reached
        var sqlCountClientsOnTrip = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tripId;";
        var sqlMaxPeople = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tripId;";

        await using var clientCountCommand = new SqlCommand(sqlCountClientsOnTrip, connection);
        clientCountCommand.Parameters.AddWithValue("@tripId", tripId);
        var clientCountResult = await clientCountCommand.ExecuteScalarAsync();
        int currentCount = Convert.ToInt32(clientCountResult);
        // Probably better to limit number of SQL requests to reduce I/O operations

        await using var maxPeopleCommand = new SqlCommand(sqlMaxPeople, connection);
        maxPeopleCommand.Parameters.AddWithValue("@tripId", tripId);
        var maxPeopleResult = await maxPeopleCommand.ExecuteScalarAsync();
        int maxPeople = Convert.ToInt32(maxPeopleResult);

        if (currentCount >= maxPeople)
            return BadRequest("This trip has reached maximum number of clients");

        // Registering client for trip FINALLY
        // payment may be null
        var sqlInsert = """
                            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                            VALUES (@clientId, @tripId, @registeredAt);
                        """;
        await using var insertCommand = new SqlCommand(sqlInsert, connection);
        insertCommand.Parameters.AddWithValue("@clientId", clientId);
        insertCommand.Parameters.AddWithValue("@tripId", tripId);
        // insertCommand.Parameters.AddWithValue("@registeredAt", DateTime.Now.Ticks); // Why are you int
        insertCommand.Parameters.AddWithValue("@registeredAt",
            (int)(DateTimeOffset.Now).ToUnixTimeSeconds()); // Why are you int

        await insertCommand.ExecuteNonQueryAsync();

        return Ok("Client registered for trip successfully, yey :D");
    }

    /**
 * Removes client from specified by id trip
 */
    [HttpDelete("{clientId}/trips/{tripId}")]
    public async Task<IActionResult> RemoveClientFromTrip(int clientId, int tripId)
    {
        var connectionString = config.GetConnectionString("Default");

        await using var connection = new SqlConnection(connectionString); // automatically closes connection when done

        // Checks if client with specified id exists in database
        var sqlClientCheck = "SELECT COUNT(*) FROM Client WHERE Client.IdClient = @clientId;";
        await using var clientCheckCommand = new SqlCommand(sqlClientCheck, connection);
        clientCheckCommand.Parameters.AddWithValue("@clientId", clientId);

        await connection.OpenAsync();
        var clientCheckResult = await clientCheckCommand.ExecuteScalarAsync();

        if (Convert.ToInt32(clientCheckResult) == 0)
        {
            // throw new NotFoundException($"Client with that Id does not exist [ClientId:{clientId}]");
            return NotFound($"Client with that Id does not exist [ClientId:{clientId}]");
        }

        // Checks if trip with specified id exists in database
        var sqlTripCheck = "SELECT COUNT(*) FROM Trip WHERE Trip.IdTrip = @tripId; ";
        await using var tripCheckCommand = new SqlCommand(sqlTripCheck, connection);
        tripCheckCommand.Parameters.AddWithValue("@tripId", tripId);

        var tripCheckResult = await tripCheckCommand.ExecuteScalarAsync();

        if (Convert.ToInt32(tripCheckResult) == 0)
        {
            // throw new NotFoundException($"Trip with that Id does not exist [TripId:{tripId}]");
            return NotFound($"Trip with that Id does not exist [TripId:{tripId}]");
        }

        var isRegisteredForTrip = """
                                      SELECT COUNT(*) FROM Client_Trip 
                                      WHERE IdClient = @clientId AND IdTrip = @tripId;
                                  """;
        await using var alreadyRegistCommand = new SqlCommand(isRegisteredForTrip, connection);
        alreadyRegistCommand.Parameters.AddWithValue("@clientId", clientId);
        alreadyRegistCommand.Parameters.AddWithValue("@tripId", tripId);
        var alreadyResult = await alreadyRegistCommand.ExecuteScalarAsync();
        var isAlready = Convert.ToInt32(alreadyResult) > 0;
        if (!isAlready)
        {
            // throw new InvalidOperationException(
            //     "He is to poor to register for trip and you want to remove him from trip? Sadeg :(");
            return Conflict("Client is not registered for this trip, can't delete registration");
        }

        // Registering client for trip FINALLY
        // payment may be null
        var sqlDelete = """
                            DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId
                        """;
        await using var insertCommand = new SqlCommand(sqlDelete, connection);
        insertCommand.Parameters.AddWithValue("@clientId", clientId);
        insertCommand.Parameters.AddWithValue("@tripId", tripId);

        await insertCommand.ExecuteNonQueryAsync();

        // https://stackoverflow.com/questions/25970523/restful-what-should-a-delete-response-body-contain
        return NoContent();
    }
}