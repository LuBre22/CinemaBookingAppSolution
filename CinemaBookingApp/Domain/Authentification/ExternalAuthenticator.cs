namespace CinemaBookingApp.Domain.Authentification;

public sealed class ExternalAuthenticator
{
    public ExternalAuthenticator(bool approve)
    {
        Approve = approve;
    }

    public bool Approve { get; }

    public bool Authenticate(Guid customerId) => Approve;
}