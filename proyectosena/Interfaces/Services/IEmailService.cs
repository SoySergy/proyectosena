namespace proyectosena.Interfaces.Services
{
    public interface IEmailService
    {
        public Task SendPasswordResetCodeAsync(string toEmail, string code);

        // Invites a newly created manager to set their own password
        public Task SendManagerInvitationAsync(string toEmail, string name, string code, int expiryMinutes);

        // Le manda al recién registrado el código para confirmar que el correo
        // existe y es suyo
        public Task SendEmailVerificationCodeAsync(string toEmail, string name, string code, int expiryMinutes);
    }
}
