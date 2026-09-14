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

        // Avisa a quien YA tiene cuenta que alguien intentó registrarse otra
        // vez con su correo o su documento (WA-03). No lleva código: no es
        // una acción que tomar, solo informa. A quien llamó al registro no se
        // le dice nada distinto de un registro nuevo.
        public Task SendAlreadyRegisteredNoticeAsync(string toEmail, string name);
    }
}
