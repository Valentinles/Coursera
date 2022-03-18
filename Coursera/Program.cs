using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Coursera
{
    public partial class StudentsCoursesXref
    {
        public string StudentPin { get; set; }
        public int CourseId { get; set; }
        public DateTime? CompletionDate { get; set; }

        public virtual Course Course { get; set; }
        public virtual Student StudentPinNavigation { get; set; }
    }

    public partial class Student
    {
        public Student()
        {
            StudentsCoursesXrefs = new HashSet<StudentsCoursesXref>();
        }

        public string Pin { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime TimeCreated { get; set; }

        public virtual ICollection<StudentsCoursesXref> StudentsCoursesXrefs { get; set; }
    }

    public partial class Instructor
    {
        public Instructor()
        {
            Courses = new HashSet<Course>();
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime TimeCreated { get; set; }

        public virtual ICollection<Course> Courses { get; set; }
    }

    public partial class Course
    {
        public Course()
        {
            StudentsCoursesXrefs = new HashSet<StudentsCoursesXref>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int InstructorId { get; set; }
        public byte TotalTime { get; set; }
        public byte Credit { get; set; }
        public DateTime TimeCreated { get; set; }

        public virtual Instructor Instructor { get; set; }
        public virtual ICollection<StudentsCoursesXref> StudentsCoursesXrefs { get; set; }
    }

    public partial class CourseraContext : DbContext
    {
        public CourseraContext()
        {
        }

        public CourseraContext(DbContextOptions<CourseraContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Instructor> Instructors { get; set; }
        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<StudentsCoursesXref> StudentsCoursesXrefs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=coursera;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Cyrillic_General_CI_AS");

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Credit).HasColumnName("credit");

                entity.Property(e => e.InstructorId).HasColumnName("instructor_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnName("name");

                entity.Property(e => e.TimeCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("time_created")
                    .HasDefaultValueSql("(getdate())");

                entity.Property(e => e.TotalTime).HasColumnName("total_time");

                entity.HasOne(d => d.Instructor)
                    .WithMany(p => p.Courses)
                    .HasForeignKey(d => d.InstructorId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_courses_instructors");
            });

            modelBuilder.Entity<Instructor>(entity =>
            {
                entity.ToTable("instructors");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("first_name");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("last_name");

                entity.Property(e => e.TimeCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("time_created")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Pin);

                entity.ToTable("students");

                entity.Property(e => e.Pin)
                    .HasMaxLength(10)
                    .HasColumnName("pin")
                    .IsFixedLength(true);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("first_name");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("last_name");

                entity.Property(e => e.TimeCreated)
                    .HasColumnType("datetime")
                    .HasColumnName("time_created")
                    .HasDefaultValueSql("(getdate())");
            });

            modelBuilder.Entity<StudentsCoursesXref>(entity =>
            {
                entity.HasKey(e => new { e.StudentPin, e.CourseId });

                entity.ToTable("students_courses_xref");

                entity.Property(e => e.StudentPin)
                    .HasMaxLength(10)
                    .HasColumnName("student_pin")
                    .IsFixedLength(true);

                entity.Property(e => e.CourseId).HasColumnName("course_id");

                entity.Property(e => e.CompletionDate)
                    .HasColumnType("date")
                    .HasColumnName("completion_date");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.StudentsCoursesXrefs)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_students_courses_xref_courses");

                entity.HasOne(d => d.StudentPinNavigation)
                    .WithMany(p => p.StudentsCoursesXrefs)
                    .HasForeignKey(d => d.StudentPin)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_students_courses_xref_students");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

    public abstract class DataService
    {
        protected readonly CourseraContext context;

        public DataService(CourseraContext context)
        {
            this.context = context;
        }
    }

    public interface IStudentService
    {
        ICollection<Student> GetStudents();
        int GetStudentTotalCredits(Student student);
        ICollection<Student> GetStudentsForReport(byte minimumCredit, string pins, DateTime startDate, DateTime endDate);

    }

    public class StudentService : DataService, IStudentService
    {
        public StudentService(CourseraContext context) : base(context)
        {
        }

        public ICollection<Student> GetStudents()
        {
            return this.context.Students
                 .Include(x => x.StudentsCoursesXrefs)
                 .ThenInclude(x => x.Course)
                 .ThenInclude(x => x.Instructor).ToList();
        }

        public ICollection<Student> GetStudentsForReport(byte minimumCredit, string pins, DateTime startDate, DateTime endDate)
        {
            ICollection<Student> students = new List<Student>();
            if (pins != "")
            {
                var separatedPins = pins.Split(",").ToList();
                foreach (var pin in separatedPins)
                {
                    var student = this.context.Students.FirstOrDefault(x => x.Pin == pin);
                    if (student != null)
                    {
                        students.Add(student);
                    }
                }
            }
            else
            {
                students = GetStudents();
            }
            var studentsForReport = new List<Student>();
            foreach (var student in students)
            {
                var studentTotalCredits = GetStudentTotalCredits(student);
                var completionDate = this.context.StudentsCoursesXrefs
                    .FirstOrDefault(x => x.StudentPinNavigation.Pin == student.Pin)
                    .CompletionDate;
                if (studentTotalCredits >= minimumCredit && completionDate >= startDate && completionDate <= endDate)
                {
                    studentsForReport.Add(student);
                }
            }

            return studentsForReport;
        }

        public int GetStudentTotalCredits(Student student)
        {
            byte totalCredits = 0;
            var studentCourses = this.context.StudentsCoursesXrefs
                .Include(x => x.StudentPinNavigation)
                .Include(x => x.Course)
                .Where(x => x.StudentPinNavigation.Pin == student.Pin && x.CompletionDate != null);
            foreach (var st in studentCourses)
            {
                totalCredits += st.Course.Credit;
            }
            return totalCredits;
        }
    }

    public interface IReportService
    {
        void GenerateCsvReport(ICollection<Student> students);
        void GenerateHtmlReport(ICollection<Student> students);
    }

    public class ReportService : DataService, IReportService
    {
        private readonly IStudentService studentService;
        public ReportService(CourseraContext context, IStudentService studentService) : base(context)
        {
            this.studentService = studentService;
        }

        public void GenerateCsvReport(ICollection<Student> students)
        {
            string path = @"C:\\report.csv";

            using (StreamWriter w = File.CreateText(path))
            {
                foreach (var student in students)
                {
                    var studentName = student.FirstName + " " + student.LastName;
                    var studentTotalCredit = this.studentService.GetStudentTotalCredits(student);
                    var headline = string.Format("{0},{1}", studentName, studentTotalCredit);
                    w.WriteLine(headline);
                    foreach (var course in student.StudentsCoursesXrefs)
                    {
                        var courseName = course.Course.Name;
                        var courseTotalTime = course.Course.TotalTime;
                        var courseCredit = course.Course.Credit;
                        var courseInstructor = course.Course.Instructor.FirstName + " " + course.Course.Instructor.LastName;
                        var courseInofmration = string.Format("{0},{1} {2}", courseName, courseTotalTime, courseInstructor);
                        w.WriteLine(courseInofmration);
                    }
                }
                w.Flush();
                w.Close();
            }
        }

        public void GenerateHtmlReport(ICollection<Student> students)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<TABLE>\n");
            sb.Append("<tr>");
            sb.Append("<th>");
            sb.Append("Student");
            sb.Append("</th>");
            sb.Append("<th>");
            sb.Append("TotalCredit");
            sb.Append("</th>");
            sb.Append("</tr>\n");
            sb.Append("<tr>");
            sb.Append("<th>");
            sb.Append(" ");
            sb.Append("</th>");
            sb.Append("<th>");
            sb.Append("CourseName");
            sb.Append("</th>");
            sb.Append("<th>");
            sb.Append("Time");
            sb.Append("</th>");
            sb.Append("<th>");
            sb.Append("Credit");
            sb.Append("</th>");
            sb.Append("<th>");
            sb.Append("Instructor");
            sb.Append("</th>");
            sb.Append("</th>");
            sb.Append("</tr>\n");
            foreach (var student in students)
            {
                sb.Append("<TR>\n");
                sb.Append("<td>");
                sb.Append(student.FirstName + " " + student.LastName);
                sb.Append("</td>");
                sb.Append("<td>");
                sb.Append(this.studentService.GetStudentTotalCredits(student));
                sb.Append("</td>");
                sb.Append("</TR>\n");
                sb.Append("<td>");
                sb.Append(" ");
                sb.Append("</td>");
                foreach (var course in student.StudentsCoursesXrefs)
                {
                    sb.Append("<TR>\n");
                    var courseName = course.Course.Name;
                    sb.Append("<td>");
                    sb.Append(" ");
                    sb.Append("</td>");
                    sb.Append("<td>");
                    sb.Append(courseName);
                    sb.Append("</td>");
                    var courseTotalTime = course.Course.TotalTime;
                    sb.Append("<td>");
                    sb.Append(courseTotalTime);
                    sb.Append("</td>");
                    var courseCredit = course.Course.Credit;
                    sb.Append("<td>");
                    sb.Append(courseCredit);
                    sb.Append("</td>");
                    var courseInstructor = course.Course.Instructor.FirstName + " " + course.Course.Instructor.LastName;
                    sb.Append("<td>");
                    sb.Append(courseInstructor);
                    sb.Append("</td>");
                    sb.Append("<TR>\n");
                }
                sb.Append("<TR>\n");
                sb.Append("</TR>\n");
            }
            sb.Append("</TABLE>");

            string path = @"C:\\report.html";

            using (StreamWriter w = File.CreateText(path))
            {
                w.WriteLine(sb);
                w.Flush();
                w.Close();
            }

        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var db = new CourseraContext();
            IStudentService studentService = new StudentService(db);
            IReportService reportService = new ReportService(db, studentService);

            Console.WriteLine("Enter students' pins: ");
            var inputPins = Console.ReadLine();

            bool validCredits = false;
            byte inputMinCredits = 0;
            while (!validCredits)
            {
                Console.WriteLine("Enter the minimum credits required: ");

                validCredits = byte.TryParse(Console.ReadLine(), out inputMinCredits)
                    && inputMinCredits > 0;
            }

            Console.Write("Enter start date (e.g. 18/3/2022): ");
            DateTime startDate = DateTime.Parse(Console.ReadLine());

            Console.Write("Enter end date (e.g. 18/3/2022): ");
            DateTime endDate = DateTime.Parse(Console.ReadLine());

            var students = studentService.GetStudentsForReport(inputMinCredits, inputPins, startDate, endDate);

            Console.WriteLine("Reported data: ");
            foreach (var student in students)
            {
                Console.WriteLine(student.FirstName);
                foreach (var course in student.StudentsCoursesXrefs)
                {
                    Console.WriteLine(course.Course.Name);

                }
                Console.WriteLine(studentService.GetStudentTotalCredits(student));
                Console.WriteLine("");
            }

            reportService.GenerateCsvReport(students);
            reportService.GenerateHtmlReport(students);
        }
    }
}
