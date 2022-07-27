using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace DanTech.Data
{
    public partial class dgdb : DbContext
    {
        public dgdb()
        {
        }

        public dgdb(DbContextOptions<dgdb> options)
            : base(options)
        {
        }

        public virtual DbSet<dtMisc> dtMiscs { get; set; }
        public virtual DbSet<dtPlanItem> dtPlanItems { get; set; }
        public virtual DbSet<dtProject> dtProjects { get; set; }
        public virtual DbSet<dtSession> dtSessions { get; set; }
        public virtual DbSet<dtTestDatum> dtTestData { get; set; }
        public virtual DbSet<dtType> dtTypes { get; set; }
        public virtual DbSet<dtUser> dtUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySQL("Name=ConnectionStrings:DG");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<dtMisc>(entity =>
            {
                entity.ToTable("dtMisc");

                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<dtPlanItem>(entity =>
            {
                entity.ToTable("dtPlanItem");

                entity.HasIndex(e => e.project, "fk_PlanItem_Project_idx");

                entity.HasIndex(e => e.user, "fk_PlanItem_User_idx");

                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.addToCalendar).HasColumnType("bit(1)");

                entity.Property(e => e.completed).HasColumnType("bit(1)");

                entity.Property(e => e.day).HasColumnType("date");

                entity.Property(e => e.priority).HasColumnType("int(11)");

                entity.Property(e => e.project).HasColumnType("int(11)");

                entity.Property(e => e.title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.user).HasColumnType("int(11)");

                entity.HasOne(d => d.projectNavigation)
                    .WithMany(p => p.dtPlanItems)
                    .HasForeignKey(d => d.project)
                    .HasConstraintName("fk_PlanItem_Project");

                entity.HasOne(d => d.userNavigation)
                    .WithMany(p => p.dtPlanItems)
                    .HasForeignKey(d => d.user)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_PlanItem_User");
            });

            modelBuilder.Entity<dtProject>(entity =>
            {
                entity.ToTable("dtProject");

                entity.HasIndex(e => e.user, "fk_project_user_idx");

                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.priority).HasColumnType("int(11)");

                entity.Property(e => e.shortCode)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.sortOrder).HasColumnType("int(11)");

                entity.Property(e => e.title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.user).HasColumnType("int(11)");

                entity.HasOne(d => d.userNavigation)
                    .WithMany(p => p.dtProjects)
                    .HasForeignKey(d => d.user)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_project_user");
            });

            modelBuilder.Entity<dtSession>(entity =>
            {
                entity.ToTable("dtSession");

                entity.HasIndex(e => e.user, "user_UNIQUE")
                    .IsUnique();

                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.hostAddress).IsRequired();

                entity.Property(e => e.session).IsRequired();

                entity.Property(e => e.user).HasColumnType("int(11)");

                entity.HasOne(d => d.userNavigation)
                    .WithOne(p => p.dtSession)
                    .HasForeignKey<dtSession>(d => d.user)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_session_user");
            });

            modelBuilder.Entity<dtTestDatum>(entity =>
            {
                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<dtType>(entity =>
            {
                entity.ToTable("dtType");

                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.title)
                    .IsRequired()
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<dtUser>(entity =>
            {
                entity.ToTable("dtUser");

                entity.HasIndex(e => e.type, "fk_users_type_ddttypes_idx");

                entity.Property(e => e.id).HasColumnType("int(11)");

                entity.Property(e => e.email).HasMaxLength(100);

                entity.Property(e => e.fName).HasMaxLength(100);

                entity.Property(e => e.lName).HasMaxLength(100);

                entity.Property(e => e.otherName).HasMaxLength(100);

                entity.Property(e => e.suspended).HasColumnType("tinyint(4)");

                entity.Property(e => e.type).HasColumnType("int(11)");

                entity.HasOne(d => d.typeNavigation)
                    .WithMany(p => p.dtUsers)
                    .HasForeignKey(d => d.type)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_users_type_ddttypes");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
