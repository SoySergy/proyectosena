namespace proyectosena.Tests.Infrastructure
{
    /// <summary>
    /// Una sola API y una sola base para todas las pruebas.
    /// </summary>
    /// <remarks>
    /// Arrancar PostgreSQL cuesta varios segundos; hacerlo por prueba multiplicaría
    /// el tiempo. Cada prueba se aísla con su propio correo y su propia IP.
    /// </remarks>
    [CollectionDefinition(Name)]
    public class ApiCollection : ICollectionFixture<RecyRouteApiFactory>
    {
        public const string Name = "API";
    }
}
