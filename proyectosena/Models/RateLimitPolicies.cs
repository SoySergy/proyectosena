namespace proyectosena.Models
{
    // Nombres de las políticas de límite de peticiones.
    //
    // Tienen que ser `const` y no `static readonly`: van dentro del atributo
    // [EnableRateLimiting("...")], que exige una constante de compilación.
    public static class RateLimitPolicies
    {
        // Intentar entrar: login y comprobación de códigos. Una persona real hace
        // uno o dos intentos; el ataque medido hacía 37 por segundo.
        public const string Auth = "auth";

        // Endpoints que MANDAN UN CORREO a una dirección que elige quien llama.
        // Sin freno sirven para inundar el buzón de una víctima y para renovar
        // códigos sin parar, saltándose el contador de intentos.
        public const string Email = "email";
    }
}
