// =============================================
// DB CONTEXT: RecyRouteDbContext
// Database: RecyRoute (Full English)
// =============================================

using Microsoft.EntityFrameworkCore;
using proyectosena.Models;

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
            // TABLE: Role
            // ══════════════════════════════════════
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");
                entity.HasKey(r => r.IdRole);
                entity.Property(r => r.IdRole)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(r => r.RoleName)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(r => r.RoleDescription)
                    .IsRequired()
                    .HasMaxLength(250);
            });

            // ══════════════════════════════════════
            // TABLE: DocumentType
            // ══════════════════════════════════════
            modelBuilder.Entity<DocumentType>(entity =>
            {
                entity.ToTable("DocumentType");
                entity.HasKey(d => d.IdDocumentType);
                entity.Property(d => d.IdDocumentType)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(d => d.DocumentName)
                    .IsRequired()
                    .HasMaxLength(30);
                entity.Property(d => d.Abbreviation)
                    .IsRequired()
                    .HasMaxLength(3);
            });

            // ══════════════════════════════════════
            // TABLE: User
            // ══════════════════════════════════════
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.IdUser);
                entity.Property(u => u.IdUser)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(u => u.IdRole)
                    .IsRequired();
                entity.Property(u => u.IdDocumentType)
                    .IsRequired();
                entity.Property(u => u.DocumentNumber)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(u => u.Name)
                    .IsRequired()
                    .HasMaxLength(70);
                entity.Property(u => u.LastName)
                    .IsRequired()
                    .HasMaxLength(70);
                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(u => u.Password)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(u => u.PhoneNumber)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(u => u.Address)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(u => u.RegistrationDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // ── Unique Constraints ─────────────
                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("UQ_User_Email");
                entity.HasIndex(u => u.DocumentNumber)
                    .IsUnique()
                    .HasDatabaseName("UQ_User_Document");

                // ── Relationships ──────────────────
                entity.HasOne(u => u.Role)
                    .WithMany(r => r.User)
                    .HasForeignKey(u => u.IdRole)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_User_Role");

                entity.HasOne(u => u.DocumentType)
                    .WithMany(d => d.User)
                    .HasForeignKey(u => u.IdDocumentType)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_User_DocType");
            });

            // ══════════════════════════════════════
            // TABLE: CollectionRequest
            // ══════════════════════════════════════
            modelBuilder.Entity<CollectionRequest>(entity =>
            {
                entity.ToTable("CollectionRequest");
                entity.HasKey(s => s.IdRequest);
                entity.Property(s => s.IdRequest)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(s => s.IdUser)
                    .IsRequired();
                entity.Property(s => s.CollectionDate)
                    .IsRequired();
                entity.Property(s => s.CollectionTime)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(s => s.CollectionAddress)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(s => s.ContactPhone)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(s => s.CurrentStatus)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");
                entity.Property(s => s.RequestDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(s => s.WasteTypes)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(s => s.CitizenObservations)
                    .HasMaxLength(500)
                    .IsRequired(false);

                // ── Relationships ──────────────────
                entity.HasOne(s => s.User)
                    .WithMany(u => u.CollectionRequests)
                    .HasForeignKey(s => s.IdUser)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_CollectionRequest_User");
            });

            // ══════════════════════════════════════
            // TABLE: CollectionManagement
            // ══════════════════════════════════════
            modelBuilder.Entity<CollectionManagement>(entity =>
            {
                entity.ToTable("CollectionManagement");
                entity.HasKey(g => g.IdManagement);
                entity.Property(g => g.IdManagement)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(g => g.IdRequest)
                    .IsRequired();
                entity.Property(g => g.IdManager)
                    .IsRequired();
                entity.Property(g => g.Status)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(g => g.StatusChangeDate)
                    .IsRequired(false);
                entity.Property(g => g.ScheduledDate)
                    .IsRequired(false);
                entity.Property(g => g.CompletionDate)
                    .IsRequired(false);
                entity.Property(g => g.ManagerObservations)
                    .HasMaxLength(200)
                    .IsRequired(false);

                // ── Relationships ──────────────────
                entity.HasOne(g => g.CollectionRequest)
                    .WithMany(s => s.CollectionManagement)
                    .HasForeignKey(g => g.IdRequest)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_CollectionManagement_CollectionRequest");

                entity.HasOne(g => g.Manager)
                    .WithMany(u => u.CollectionManagement)
                    .HasForeignKey(g => g.IdManager)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_CollectionManagement_User");
            });

            // ══════════════════════════════════════
            // TABLE: Notification
            // ══════════════════════════════════════
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notification");
                entity.HasKey(n => n.IdNotification);
                entity.Property(n => n.IdNotification)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(n => n.IdUser)
                    .IsRequired(false);
                entity.Property(n => n.IdRequest)
                    .IsRequired(false);
                entity.Property(n => n.Title)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(n => n.Type)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(n => n.CreationDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(n => n.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                // ── Relationships ──────────────────
                entity.HasOne(n => n.User)
                    .WithMany(u => u.Notification)
                    .HasForeignKey(n => n.IdUser)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Notification_User");

                entity.HasOne(n => n.CollectionRequest)
                    .WithMany(s => s.Notification)
                    .HasForeignKey(n => n.IdRequest)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_Notification_CollectionRequest");
            });

            // ══════════════════════════════════════
            // TABLE: History
            // ══════════════════════════════════════
            modelBuilder.Entity<History>(entity =>
            {
                entity.ToTable("History");
                entity.HasKey(h => h.IdHistory);
                entity.Property(h => h.IdHistory)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(h => h.IdRequest)
                    .IsRequired();
                entity.Property(h => h.IdUser)
                    .IsRequired();
                entity.Property(h => h.PreviousStatus)
                    .HasMaxLength(20)
                    .IsRequired(false);
                entity.Property(h => h.NewStatus)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(h => h.ChangeDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(h => h.Comment)
                    .HasMaxLength(500)
                    .IsRequired(false);

                // ── Relationships ──────────────────
                entity.HasOne(h => h.CollectionRequest)
                    .WithMany(s => s.History)
                    .HasForeignKey(h => h.IdRequest)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_History_CollectionRequest");

                entity.HasOne(h => h.User)
                    .WithMany(u => u.History)
                    .HasForeignKey(h => h.IdUser)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_History_User");
            });

            // ══════════════════════════════════════
            // TABLE: ChatHistory
            // ══════════════════════════════════════
            modelBuilder.Entity<ChatHistory>(entity =>
            {
                entity.ToTable("ChatHistory");
                entity.HasKey(c => c.IdChatHistory);
                entity.Property(c => c.IdChatHistory)
                    .HasDefaultValueSql("NEWID()");
                entity.Property(c => c.IdRequest)
                    .IsRequired();
                entity.Property(c => c.IdSender)
                    .IsRequired();
                entity.Property(c => c.Message)
                    .IsRequired()
                    .HasMaxLength(1000);
                entity.Property(c => c.SendDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(c => c.IsRead)
                    .IsRequired()
                    .HasDefaultValue(false);

                // ── Relationships ──────────────────
                entity.HasOne(c => c.CollectionRequest)
                    .WithMany(s => s.ChatHistory)
                    .HasForeignKey(c => c.IdRequest)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_ChatHistory_CollectionRequest");

                entity.HasOne(c => c.Sender)
                    .WithMany(u => u.ChatHistory)
                    .HasForeignKey(c => c.IdSender)
                    .OnDelete(DeleteBehavior.NoAction)
                    .HasConstraintName("FK_ChatHistory_User");
            });
        }
    }
}
