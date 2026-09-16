namespace proyectosena.Interfaces.Services
{
    public interface IEmailService
    {
        public Task SendPasswordResetCodeAsync(string toEmail, string code);

        // Invita al gestor recién creado a poner su propia contraseña.
        //
        // Es el único envío que devuelve si salió o no. El resto se llama desde
        // flujos anónimos, donde decir «ese correo no existe» delataría quién
        // tiene cuenta; este lo pide un administrador autenticado sobre una
        // cuenta que él mismo acaba de crear, así que saberlo no revela nada y
        // le evita quedarse esperando a alguien que nunca recibió su código.
        public Task<bool> SendManagerInvitationAsync(string toEmail, string name, string code, int expiryMinutes);

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
