namespace APBD_07.Models;

public class ClientTrip
{
    public required int IdClient;
    public required int IdTrip;
    public required int RegisteredAt;
    public int? PaymentDate; // I guess it may be nullable based on db diagram
}