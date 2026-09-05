namespace proyectosena.Repositories.Interfaces
{
    public interface IEmailService
    {
        public Task SendPasswordResetCodeAsync(string toEmail, string code);

        // Invites a newly created manager to set their own password
        public Task SendManagerInvitationAsync(string toEmail, string name, string code, int expiryMinutes);
    }
}
