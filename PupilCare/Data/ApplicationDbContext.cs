using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PupilCare.Models;

namespace PupilCare.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Core school structure
        public DbSet<School> Schools { get; set; }
        public DbSet<ClassLevel> ClassLevels { get; set; }
        public DbSet<ClassSection> ClassSections { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TeacherAssignment> TeacherAssignments { get; set; }

        // Students
        public DbSet<Student> Students { get; set; }
        public DbSet<StudentComment> StudentComments { get; set; }

        // Academic records
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamMark> ExamMarks { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        // AI & Subscription
        public DbSet<AiInsight> AiInsights { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── School → ClassLevel ──────────────────────────────────────────────
            builder.Entity<ClassLevel>()
                .HasOne(cl => cl.School)
                .WithMany(s => s.ClassLevels)
                .HasForeignKey(cl => cl.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);

            // Class teacher is optional and must not cascade-delete the class
            builder.Entity<ClassLevel>()
                .HasOne(cl => cl.ClassTeacher)
                .WithMany()
                .HasForeignKey(cl => cl.ClassTeacherId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // ── ClassLevel → ClassSection ────────────────────────────────────────
            builder.Entity<ClassSection>()
                .HasOne(cs => cs.ClassLevel)
                .WithMany(cl => cl.Sections)
                .HasForeignKey(cs => cs.ClassLevelId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── ClassLevel → Subject ─────────────────────────────────────────────
            builder.Entity<Subject>()
                .HasOne(s => s.ClassLevel)
                .WithMany(cl => cl.Subjects)
                .HasForeignKey(s => s.ClassLevelId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── TeacherAssignment (Teacher × Section × Subject) ──────────────────
            builder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Teacher)
                .WithMany()
                .HasForeignKey(ta => ta.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.ClassSection)
                .WithMany(cs => cs.TeacherAssignments)
                .HasForeignKey(ta => ta.ClassSectionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Subject)
                .WithMany(s => s.TeacherAssignments)
                .HasForeignKey(ta => ta.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // One teacher per section-subject
            builder.Entity<TeacherAssignment>()
                .HasIndex(ta => new { ta.ClassSectionId, ta.SubjectId })
                .IsUnique();

            // ── Student → ClassSection ───────────────────────────────────────────
            builder.Entity<Student>()
                .HasOne(s => s.ClassSection)
                .WithMany(cs => cs.Students)
                .HasForeignKey(s => s.ClassSectionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── ExamMark ─────────────────────────────────────────────────────────
            builder.Entity<ExamMark>()
                .HasOne(em => em.Student)
                .WithMany(s => s.ExamMarks)
                .HasForeignKey(em => em.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ExamMark>()
                .HasOne(em => em.Exam)
                .WithMany(e => e.Marks)
                .HasForeignKey(em => em.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ExamMark>()
                .HasOne(em => em.Subject)
                .WithMany(s => s.ExamMarks)
                .HasForeignKey(em => em.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ExamMark>()
                .HasOne(em => em.GradedBy)
                .WithMany()
                .HasForeignKey(em => em.GradedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Unique: one mark per student per exam per subject
            builder.Entity<ExamMark>()
                .HasIndex(em => new { em.ExamId, em.StudentId, em.SubjectId })
                .IsUnique();

            // ── AttendanceRecord ──────────────────────────────────────────────────
            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.Student)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(ar => ar.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.Subject)
                .WithMany(s => s.AttendanceRecords)
                .HasForeignKey(ar => ar.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.ClassSection)
                .WithMany(cs => cs.AttendanceRecords)
                .HasForeignKey(ar => ar.ClassSectionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AttendanceRecord>()
                .HasOne(ar => ar.TakenBy)
                .WithMany()
                .HasForeignKey(ar => ar.TakenByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Unique: one attendance entry per student per subject per day
            builder.Entity<AttendanceRecord>()
                .HasIndex(ar => new { ar.StudentId, ar.SubjectId, ar.Date })
                .IsUnique();

            // ── StudentComment ────────────────────────────────────────────────────
            builder.Entity<StudentComment>()
                .HasOne(sc => sc.Student)
                .WithMany(s => s.Comments)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentComment>()
                .HasOne(sc => sc.CreatedBy)
                .WithMany()
                .HasForeignKey(sc => sc.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<StudentComment>()
                .HasOne(sc => sc.Subject)
                .WithMany(s => s.Comments)
                .HasForeignKey(sc => sc.SubjectId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            builder.Entity<StudentComment>()
                .HasOne(sc => sc.ClassSection)
                .WithMany(cs => cs.Comments)
                .HasForeignKey(sc => sc.ClassSectionId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // ── Exam ─────────────────────────────────────────────────────────────
            builder.Entity<Exam>()
                .HasOne(e => e.ClassLevel)
                .WithMany(cl => cl.Exams)
                .HasForeignKey(e => e.ClassLevelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Exam>()
                .HasOne(e => e.School)
                .WithMany(s => s.Exams)
                .HasForeignKey(e => e.SchoolId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── AiInsight ─────────────────────────────────────────────────────────
            builder.Entity<AiInsight>()
                .HasOne(ai => ai.GeneratedBy)
                .WithMany()
                .HasForeignKey(ai => ai.GeneratedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AiInsight>()
                .HasOne(ai => ai.School)
                .WithMany(s => s.AiInsights)
                .HasForeignKey(ai => ai.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── SubscriptionPayment ───────────────────────────────────────────────
            builder.Entity<SubscriptionPayment>()
                .HasOne(sp => sp.School)
                .WithMany(s => s.SubscriptionPayments)
                .HasForeignKey(sp => sp.SchoolId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SubscriptionPayment>()
                .HasOne(sp => sp.Plan)
                .WithMany(p => p.Payments)
                .HasForeignKey(sp => sp.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision for money fields
            builder.Entity<SubscriptionPlan>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<SubscriptionPayment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            builder.Entity<ExamMark>()
                .Property(em => em.MarksObtained)
                .HasPrecision(6, 2);
        }
    }
}
