// =============================================
// DB CONTEXT: RecyRouteDbContext
// Base de datos: RecyRoute
// =============================================

using Microsoft.EntityFrameworkCore;
using proyectosena.Modelos;


namespace proyectosena.Context
{
    public class RecyRouteDbContext : DbContext
    {
        public RecyRouteDbContext(DbContextOptions<RecyRouteDbContext> options)
            : base(options) { }

        // ── DbSets ─────────────────────────────────
        public DbSet<Role> Roles { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<CollectionRequest> CollectionRequests { get; set; }
        public DbSet<CollectionManagement> CollectionManagements { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<History> Histories { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ══════════════════════════════════════
            // TABLA: Rol
            // ══════════════════════════════════════
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Rol");

                entity.HasKey(r => r.IdRole);

                entity.Property(r => r.IdRole)
                    .HasColumnName("IdRol")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(r => r.RoleName)
                    .HasColumnName("NombreRol")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(r => r.RoleDescription)
                    .HasColumnName("DescripcionRol")
                    .IsRequired()
                    .HasMaxLength(250);
            });

            // ══════════════════════════════════════
            // TABLA: TipoDocumento
            // ══════════════════════════════════════
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.ToTable("TipoDocumento");

                entity.HasKey(d => d.IdDocumentType);

                entity.Property(d => d.IdDocumentType)
                    .HasColumnName("IdTipoDocumento")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(d => d.DocumentName)
                    .HasColumnName("NombreDocumento")
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(d => d.Abbreviation)
                    .HasColumnName("Abreviatura")
                    .IsRequired()
                    .HasMaxLength(3);
            });

            // ══════════════════════════════════════
            // TABLA: Usuario
            // ══════════════════════════════════════
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Usuario");

                entity.HasKey(u => u.IdUser);

                entity.Property(u => u.IdUser)
                    .HasColumnName("IdUsuario")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(u => u.IdRole)
                    .HasColumnName("IdRol")
                    .IsRequired();

                entity.Property(u => u.IdDocumentType)
                    .HasColumnName("IdTipoDocumento")
                    .IsRequired();

                entity.Property(u => u.DocumentNumber)
                    .HasColumnName("NumeroDocumento")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(u => u.Name)
                    .HasColumnName("Nombre")
                    .IsRequired()
                    .HasMaxLength(70);

                entity.Property(u => u.LastName)
                    .HasColumnName("Apellido")
                    .IsRequired()
                    .HasMaxLength(70);

                entity.Property(u => u.Email)
                    .HasColumnName("Correo")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Password)
                    .HasColumnName("Contrasena")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(u => u.PhoneNumber)
                    .HasColumnName("NumeroDeTelefono")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(u => u.Address)
                    .HasColumnName("Direccion")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(u => u.RegistrationDate)
                    .HasColumnName("FechaRegistro")
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                // ── Unique Constraints ─────────────
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("UQ_Usuario_Correo");

                entity.HasIndex(u => u.DocumentNumber)
                    .IsUnique()
                    .HasDatabaseName("UQ_Usuario_NumeroDocumento");

                // ── Relaciones ─────────────────────
                // FK_Usuario_Rol → ON DELETE NO ACTION
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.IdRole)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Usuario_Rol");

                // FK_Usuario_TipoDocumento → ON DELETE NO ACTION
                entity.HasOne(u => u.DocumentType)
                    .WithMany(d => d.Users)
                    .HasForeignKey(u => u.IdDocumentType)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Usuario_TipoDocumento");
            });

            // ══════════════════════════════════════
            // TABLA: SolicitudRecoleccion
            // ══════════════════════════════════════
            modelBuilder.Entity<CollectionRequest>(entity =>
            {
                entity.ToTable("SolicitudRecoleccion");

                entity.HasKey(s => s.IdRequest);

                entity.Property(s => s.IdRequest)
                    .HasColumnName("IdSolicitud")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(s => s.IdUser)
                    .HasColumnName("IdUsuario")
                    .IsRequired();

                entity.Property(s => s.CollectionDate)
                    .HasColumnName("FechaDeRecoleccion")
                    .IsRequired();

                entity.Property(s => s.CollectionTime)
                    .HasColumnName("HoraDeRecoleccion")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(s => s.CollectionAddress)
                    .HasColumnName("DireccionRecoleccion")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(s => s.ContactPhone)
                    .HasColumnName("TelefonoContacto")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(s => s.CurrentStatus)
                    .HasColumnName("EstadoActual")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pendiente");

                entity.Property(s => s.RequestDate)
                    .HasColumnName("FechaSolicitud")
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(s => s.WasteTypes)
                    .HasColumnName("TipoResiduos")
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(s => s.CitizenObservations)
                    .HasColumnName("ObservacionesCiudadano")
                    .HasMaxLength(500)
                    .IsRequired(false);

                // ── Relaciones ─────────────────────
                // FK_SolicitudRecoleccion_Usuario → ON DELETE NO ACTION
                entity.HasOne(s => s.User)
                    .WithMany(u => u.CollectionRequests)
                    .HasForeignKey(s => s.IdUser)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_SolicitudRecoleccion_Usuario");
            });

            // ══════════════════════════════════════
            // TABLA: GestionRecoleccion
            // ══════════════════════════════════════
            modelBuilder.Entity<CollectionManagement>(entity =>
            {
                entity.ToTable("GestionRecoleccion");

                entity.HasKey(g => g.IdManagement);

                entity.Property(g => g.IdManagement)
                    .HasColumnName("IdGestion")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(g => g.IdRequest)
                    .HasColumnName("IdSolicitud")
                    .IsRequired();

                entity.Property(g => g.IdManager)
                    .HasColumnName("IdGestor")
                    .IsRequired();

                entity.Property(g => g.Status)
                    .HasColumnName("Estado")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(g => g.StatusChangeDate)
                    .HasColumnName("FechaCambioEstado")
                    .IsRequired(false);

                entity.Property(g => g.ScheduledDate)
                    .HasColumnName("FechaProgramada")
                    .IsRequired(false);

                entity.Property(g => g.CompletionDate)
                    .HasColumnName("FechaRealizacion")
                    .IsRequired(false);

                entity.Property(g => g.ManagerObservations)
                    .HasColumnName("ObservacionesGestor")
                    .HasMaxLength(200)
                    .IsRequired(false);

                // ── Relaciones ─────────────────────
                // FK_GestionRecoleccion_SolicitudRecoleccion → ON DELETE NO ACTION
                entity.HasOne(g => g.CollectionRequest)
                    .WithMany(s => s.CollectionManagements)
                    .HasForeignKey(g => g.IdRequest)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_GestionRecoleccion_SolicitudRecoleccion");

                // FK_GestionRecoleccion_Usuario → ON DELETE NO ACTION
                entity.HasOne(g => g.Manager)
                    .WithMany(u => u.ManagedCollections)
                    .HasForeignKey(g => g.IdManager)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_GestionRecoleccion_Usuario");
            });

            // ══════════════════════════════════════
            // TABLA: Notificacion
            // ══════════════════════════════════════
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notificacion");

                entity.HasKey(n => n.IdNotification);

                entity.Property(n => n.IdNotification)
                    .HasColumnName("IdNotificacion")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(n => n.IdUser)
                    .HasColumnName("IdUsuario")
                    .IsRequired(false);

                entity.Property(n => n.IdRequest)
                    .HasColumnName("IdSolicitud")
                    .IsRequired(false);

                entity.Property(n => n.Title)
                    .HasColumnName("Titulo")
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(n => n.Message)
                    .HasColumnName("Mensaje")
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(n => n.Type)
                    .HasColumnName("Tipo")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(n => n.CreationDate)
                    .HasColumnName("FechaCreacion")
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(n => n.IsRead)
                    .HasColumnName("Leida")
                    .IsRequired()
                    .HasDefaultValue(false);

                // ── Relaciones ─────────────────────
                // FK_Notificacion_Usuario → ON DELETE CASCADE
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notifications)
                    .HasForeignKey(n => n.IdUser)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Notificacion_Usuario");

                // FK_Notificacion_SolicitudRecoleccion → ON DELETE CASCADE
                entity.HasOne(n => n.CollectionRequest)
                    .WithMany(s => s.Notifications)
                    .HasForeignKey(n => n.IdRequest)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Notificacion_SolicitudRecoleccion");
            });

            // ══════════════════════════════════════
            // TABLA: Historial
            // ══════════════════════════════════════
            modelBuilder.Entity<History>(entity =>
            {
                entity.ToTable("Historial");

                entity.HasKey(h => h.IdHistory);

                entity.Property(h => h.IdHistory)
                    .HasColumnName("IdHistorial")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(h => h.IdRequest)
                    .HasColumnName("IdSolicitud")
                    .IsRequired();

                entity.Property(h => h.IdUser)
                    .HasColumnName("IdUsuario")
                    .IsRequired();

                entity.Property(h => h.PreviousStatus)
                    .HasColumnName("EstadoAnterior")
                    .HasMaxLength(20)
                    .IsRequired(false);

                entity.Property(h => h.NewStatus)
                    .HasColumnName("EstadoNuevo")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(h => h.ChangeDate)
                    .HasColumnName("FechaCambio")
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(h => h.Comment)
                    .HasColumnName("Comentario")
                    .HasMaxLength(500)
                    .IsRequired(false);

                // ── Relaciones ─────────────────────
                // FK_Historial_SolicitudRecoleccion → ON DELETE CASCADE
                entity.HasOne(h => h.CollectionRequest)
                    .WithMany(s => s.Histories)
                    .HasForeignKey(h => h.IdRequest)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Historial_SolicitudRecoleccion");

                // FK_Historial_Usuario → ON DELETE NO ACTION
                entity.HasOne(h => h.User)
                    .WithMany(u => u.Histories)
                    .HasForeignKey(h => h.IdUser)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_Historial_Usuario");
            });

            // ══════════════════════════════════════
            // TABLA: HistorialChat
            // ══════════════════════════════════════
            modelBuilder.Entity<ChatHistory>(entity =>
            {
                entity.ToTable("HistorialChat");

                entity.HasKey(c => c.IdChatHistory);

                entity.Property(c => c.IdChatHistory)
                    .HasColumnName("IdHistorialChat")
                    .HasDefaultValueSql("NEWID()");

                entity.Property(c => c.IdRequest)
                    .HasColumnName("IdSolicitud")
                    .IsRequired();

                entity.Property(c => c.IdSender)
                    .HasColumnName("IdEmisor")
                    .IsRequired();

                entity.Property(c => c.Message)
                    .HasColumnName("Mensaje")
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(c => c.SendDate)
                    .HasColumnName("FechaEnvio")
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(c => c.IsRead)
                    .HasColumnName("Leido")
                    .IsRequired()
                    .HasDefaultValue(false);

                // ── Relaciones ─────────────────────
                // FK_HistorialChat_SolicitudRecoleccion → ON DELETE CASCADE
                entity.HasOne(c => c.CollectionRequest)
                    .WithMany(s => s.ChatHistories)
                    .HasForeignKey(c => c.IdRequest)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_HistorialChat_SolicitudRecoleccion");

                // FK_HistorialChat_Usuario → ON DELETE NO ACTION
                entity.HasOne(c => c.Sender)
                    .WithMany(u => u.ChatMessages)
                    .HasForeignKey(c => c.IdSender)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_HistorialChat_Usuario");
            });
        }
    }
}