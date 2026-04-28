using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PupilCare.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PupilCare.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ── 1. Roles ─────────────────────────────────────────────────────────
            string[] roles = { "SuperAdmin", "SchoolAdmin", "Teacher" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            // ── 2. Super Admin ────────────────────────────────────────────────────
            var superAdmin = await EnsureUser(userManager, new ApplicationUser
            {
                UserName = "superadmin@pupilcare.com",
                Email = "superadmin@pupilcare.com",
                FullName = "Super Administrator",
                EmailConfirmed = true,
                IsActive = true
            }, "Admin@123!", "SuperAdmin");

            // ── 3. Subscription Plans ─────────────────────────────────────────────
            if (!context.SubscriptionPlans.Any())
            {
                context.SubscriptionPlans.AddRange(
                    new SubscriptionPlan
                    {
                        Name = "Trial",
                        Price = 0,
                        DurationDays = 0,
                        DurationMinutes = 5,
                        Description = "5-minute trial access to explore the system.",
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Name = "Monthly",
                        Price = 10000,
                        DurationDays = 30,
                        DurationMinutes = 0,
                        Description = "Full access for 30 days. ৳10,000/month.",
                        IsActive = true
                    },
                    new SubscriptionPlan
                    {
                        Name = "Yearly",
                        Price = 100000,
                        DurationDays = 365,
                        DurationMinutes = 0,
                        Description = "Full access for 365 days. Save over 15% vs monthly. ৳1,00,000/year.",
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // ── 4. Demo School ────────────────────────────────────────────────────
            School school;
            if (!context.Schools.Any())
            {
                school = new School
                {
                    Name = "Greenfield Academy",
                    Address = "123 Education Lane, Dhaka",
                    Phone = "+880-2-12345678",
                    Email = "info@greenfield.edu",
                    Website = "www.greenfield.edu",
                    IsApproved = true,
                    SubscriptionExpiry = DateTime.UtcNow.AddYears(1)
                };
                context.Schools.Add(school);
                await context.SaveChangesAsync();
            }
            else
            {
                school = context.Schools.First();
            }

            // ── 5. School Admin ───────────────────────────────────────────────────
            var schoolAdmin = await EnsureUser(userManager, new ApplicationUser
            {
                UserName = "admin@greenfield.edu",
                Email = "admin@greenfield.edu",
                FullName = "Greenfield Admin",
                EmailConfirmed = true,
                SchoolId = school.Id,
                Designation = "School Administrator",
                IsActive = true
            }, "School@123", "SchoolAdmin");

            // ── 6. Class Levels ───────────────────────────────────────────────────
            ClassLevel classSix, classSeven, classEight;
            if (!context.ClassLevels.Any())
            {
                classSix = new ClassLevel { Name = "Six", Order = 6, SchoolId = school.Id };
                classSeven = new ClassLevel { Name = "Seven", Order = 7, SchoolId = school.Id };
                classEight = new ClassLevel { Name = "Eight", Order = 8, SchoolId = school.Id };
                context.ClassLevels.AddRange(classSix, classSeven, classEight);
                await context.SaveChangesAsync();
            }
            else
            {
                classSix = context.ClassLevels.First(c => c.Name == "Six");
                classSeven = context.ClassLevels.First(c => c.Name == "Seven");
                classEight = context.ClassLevels.First(c => c.Name == "Eight");
            }

            // ── 7. Sections (A, B per class) ──────────────────────────────────────
            if (!context.ClassSections.Any())
            {
                context.ClassSections.AddRange(
                    new ClassSection { Name = "A", ClassLevelId = classSix.Id },
                    new ClassSection { Name = "B", ClassLevelId = classSix.Id },
                    new ClassSection { Name = "A", ClassLevelId = classSeven.Id },
                    new ClassSection { Name = "B", ClassLevelId = classSeven.Id },
                    new ClassSection { Name = "A", ClassLevelId = classEight.Id },
                    new ClassSection { Name = "B", ClassLevelId = classEight.Id }
                );
                await context.SaveChangesAsync();
            }

            var sixA = context.ClassSections.First(s => s.ClassLevelId == classSix.Id && s.Name == "A");
            var sixB = context.ClassSections.First(s => s.ClassLevelId == classSix.Id && s.Name == "B");
            var sevenA = context.ClassSections.First(s => s.ClassLevelId == classSeven.Id && s.Name == "A");

            // ── 8. Subjects ───────────────────────────────────────────────────────
            if (!context.Subjects.Any())
            {
                string[] subjectNames = { "Bangla", "English", "Mathematics", "Science", "Social Studies" };
                foreach (var level in new[] { classSix, classSeven, classEight })
                    foreach (var name in subjectNames)
                        context.Subjects.Add(new Subject { Name = name, ClassLevelId = level.Id });
                await context.SaveChangesAsync();
            }

            var bangla6 = context.Subjects.First(s => s.ClassLevelId == classSix.Id && s.Name == "Bangla");
            var english6 = context.Subjects.First(s => s.ClassLevelId == classSix.Id && s.Name == "English");
            var math6 = context.Subjects.First(s => s.ClassLevelId == classSix.Id && s.Name == "Mathematics");
            var bangla7 = context.Subjects.First(s => s.ClassLevelId == classSeven.Id && s.Name == "Bangla");
            var english7 = context.Subjects.First(s => s.ClassLevelId == classSeven.Id && s.Name == "English");

            // ── 9. Teachers ───────────────────────────────────────────────────────
            var teacher1 = await EnsureUser(userManager, new ApplicationUser
            {
                UserName = "teacher1@greenfield.edu",
                Email = "teacher1@greenfield.edu",
                FullName = "Rahim Uddin",
                EmailConfirmed = true,
                SchoolId = school.Id,
                Designation = "Senior Teacher",
                IsActive = true
            }, "Teacher@123", "Teacher");

            var teacher2 = await EnsureUser(userManager, new ApplicationUser
            {
                UserName = "teacher2@greenfield.edu",
                Email = "teacher2@greenfield.edu",
                FullName = "Fatema Begum",
                EmailConfirmed = true,
                SchoolId = school.Id,
                Designation = "Teacher",
                IsActive = true
            }, "Teacher@123", "Teacher");

            // ── 10. Teacher Assignments ───────────────────────────────────────────
            if (!context.TeacherAssignments.Any())
            {
                context.TeacherAssignments.AddRange(
                    // Teacher1: Bangla + English in Class 6 Section A
                    new TeacherAssignment { TeacherId = teacher1.Id, ClassSectionId = sixA.Id, SubjectId = bangla6.Id },
                    new TeacherAssignment { TeacherId = teacher1.Id, ClassSectionId = sixA.Id, SubjectId = english6.Id },
                    // Teacher1: Bangla in Class 7 Section A
                    new TeacherAssignment { TeacherId = teacher1.Id, ClassSectionId = sevenA.Id, SubjectId = bangla7.Id },
                    // Teacher2: Math in Class 6 Section A
                    new TeacherAssignment { TeacherId = teacher2.Id, ClassSectionId = sixA.Id, SubjectId = math6.Id },
                    // Teacher2: English in Class 7 Section A
                    new TeacherAssignment { TeacherId = teacher2.Id, ClassSectionId = sevenA.Id, SubjectId = english7.Id }
                );
                await context.SaveChangesAsync();
            }

            // ── 11. Set Teacher1 as Class Teacher for Class Six ───────────────────
            if (classSix.ClassTeacherId == null)
            {
                classSix.ClassTeacherId = teacher1.Id;
                await context.SaveChangesAsync();
            }

            // ── 12. Exams ─────────────────────────────────────────────────────────
            if (!context.Exams.Any())
            {
                context.Exams.AddRange(
                    new Exam { Name = "First Term", FullMark = 100, ClassLevelId = classSix.Id, SchoolId = school.Id, CreatedByUserId = schoolAdmin.Id },
                    new Exam { Name = "Mid Term", FullMark = 50, ClassLevelId = classSix.Id, SchoolId = school.Id, CreatedByUserId = schoolAdmin.Id },
                    new Exam { Name = "Quiz 1", FullMark = 10, ClassLevelId = classSix.Id, SchoolId = school.Id, CreatedByUserId = schoolAdmin.Id },
                    new Exam { Name = "Final", FullMark = 100, ClassLevelId = classSeven.Id, SchoolId = school.Id, CreatedByUserId = schoolAdmin.Id }
                );
                await context.SaveChangesAsync();
            }

            // ── 13. Students ──────────────────────────────────────────────────────
            if (!context.Students.Any())
            {
                var studentsInSixA = new[]
                {
                    ("GFA-001", "Arif Hossain", "Male", sixA.Id),
                    ("GFA-002", "Nadia Islam", "Female", sixA.Id),
                    ("GFA-003", "Karim Ahmed", "Male", sixA.Id),
                    ("GFA-004", "Sumaiya Akter", "Female", sixA.Id),
                    ("GFA-005", "Rashed Khan", "Male", sixA.Id),
                };
                var studentsInSixB = new[]
                {
                    ("GFA-006", "Mehedi Hasan", "Male", sixB.Id),
                    ("GFA-007", "Tania Parvin", "Female", sixB.Id),
                    ("GFA-008", "Imran Hossain", "Male", sixB.Id),
                };

                foreach (var (sid, name, gender, sectionId) in studentsInSixA.Concat(studentsInSixB))
                {
                    context.Students.Add(new Student
                    {
                        StudentId = sid,
                        Name = name,
                        Gender = gender,
                        ClassSectionId = sectionId,
                        DateOfBirth = new DateTime(2012, 1, 1),
                        IsActive = true
                    });
                }
                await context.SaveChangesAsync();

                // Add some sample exam marks and attendance
                var exam = context.Exams.First(e => e.Name == "First Term");
                var students6A = context.Students.Where(s => s.ClassSectionId == sixA.Id).ToList();
                var rnd = new Random(42);

                foreach (var student in students6A)
                {
                    foreach (var subject in new[] { bangla6, english6, math6 })
                    {
                        context.ExamMarks.Add(new ExamMark
                        {
                            ExamId = exam.Id,
                            StudentId = student.Id,
                            SubjectId = subject.Id,
                            MarksObtained = rnd.Next(50, 100),
                            GradedByUserId = teacher1.Id,
                            GradedAt = DateTime.UtcNow.AddDays(-10)
                        });
                    }
                }

                // Sample attendance for the last 7 days
                for (int day = 7; day >= 1; day--)
                {
                    var date = DateTime.UtcNow.Date.AddDays(-day);
                    foreach (var student in students6A)
                    {
                        context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            StudentId = student.Id,
                            SubjectId = bangla6.Id,
                            ClassSectionId = sixA.Id,
                            Date = date,
                            IsPresent = rnd.Next(0, 5) > 0, // 80% attendance
                            TakenByUserId = teacher1.Id
                        });
                    }
                }

                // Sample comments
                context.StudentComments.AddRange(
                    new StudentComment
                    {
                        StudentId = students6A[0].Id,
                        CommentType = "Behavior",
                        Text = "Arif is very attentive in class and always participates actively.",
                        CreatedByUserId = teacher1.Id,
                        ClassSectionId = sixA.Id
                    },
                    new StudentComment
                    {
                        StudentId = students6A[1].Id,
                        CommentType = "Result",
                        Text = "Nadia has shown significant improvement in Mathematics this term.",
                        CreatedByUserId = teacher2.Id,
                        ClassSectionId = sixA.Id,
                        SubjectId = math6.Id
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        private static async Task<ApplicationUser> EnsureUser(
            UserManager<ApplicationUser> userManager,
            ApplicationUser template,
            string password,
            string role)
        {
            var existing = await userManager.FindByEmailAsync(template.Email!);
            if (existing != null) return existing;

            var result = await userManager.CreateAsync(template, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(template, role);

            return template;
        }
    }
}
