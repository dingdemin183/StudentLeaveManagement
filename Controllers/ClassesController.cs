using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api/classes")]
    [ApiController]
    public class ClassesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClassesController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "管理员")]
        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] ClassCreateRequest request)
        {
            var @class = new Class
            {
                Cid = request.cid,
                Cname = request.cname,
                Grade = request.grade,
                Did = request.department?.did ?? string.Empty,
                Tid = request.classTeacher?.tid
            };

            _context.Classes.Add(@class);
            await _context.SaveChangesAsync();
            return Ok(@class);
        }

        public class ClassCreateRequest
        {
            public string cid { get; set; } = string.Empty;
            public string cname { get; set; } = string.Empty;
            public string grade { get; set; } = string.Empty;
            public DepartmentInfo? department { get; set; }
            public TeacherInfo? classTeacher { get; set; }
        }

        public class DepartmentInfo
        {
            public string did { get; set; } = string.Empty;
            public string? dname { get; set; }
        }

        public class TeacherInfo
        {
            public string tid { get; set; } = string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClasses()
        {
            var classes = await _context.Classes
                .Include(c => c.TidNavigation)
                .Include(c => c.DidNavigation)
                .ToListAsync();

            var result = classes.Select(c => new
            {
                cid = c.Cid,
                cname = c.Cname,
                grade = c.Grade,
                did = c.Did,
                tid = c.Tid,
                studentCount = _context.Students.Count(s => s.Cid == c.Cid),
                classTeacher = c.TidNavigation != null ? new
                {
                    tid = c.TidNavigation.Tid,
                    tname = c.TidNavigation.Tname
                } : null,
                departmentName = c.DidNavigation?.Dname
            });

            return Ok(result);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<IActionResult> GetClassesByDepartment(string departmentId)
        {
            var classes = await _context.Classes
                .Include(c => c.TidNavigation)
                .Include(c => c.DidNavigation)
                .Where(c => c.Did == departmentId)
                .ToListAsync();

            var result = classes.Select(c => new
            {
                cid = c.Cid,
                cname = c.Cname,
                grade = c.Grade,
                did = c.Did,
                tid = c.Tid,
                studentCount = _context.Students.Count(s => s.Cid == c.Cid),
                classTeacher = c.TidNavigation != null ? new
                {
                    tid = c.TidNavigation.Tid,
                    tname = c.TidNavigation.Tname
                } : null,
                departmentName = c.DidNavigation?.Dname
            });

            return Ok(result);
        }

        [HttpGet("teacher/{teacherId}")]
        public async Task<IActionResult> GetClassesByTeacher(string teacherId)
        {
            var classes = await _context.Classes
                .Where(c => c.Tid == teacherId)
                .ToListAsync();
            return Ok(classes);
        }

        [HttpGet("{classId}")]
        public async Task<IActionResult> GetClassById(string classId)
        {
            var @class = await _context.Classes.FindAsync(classId);
            if (@class == null)
            {
                return NotFound();
            }
            return Ok(@class);
        }

        [HttpPut("{classId}")]
        public async Task<IActionResult> UpdateClass(string classId, [FromBody] ClassCreateRequest request)
        {
            var existingClass = await _context.Classes.FindAsync(classId);
            if (existingClass == null)
            {
                return NotFound();
            }

            // 更新班级信息
            existingClass.Cname = request.cname;
            existingClass.Grade = request.grade;
            existingClass.Did = request.department?.did ?? existingClass.Did;
            existingClass.Tid = request.classTeacher?.tid;

            await _context.SaveChangesAsync();
            return Ok(existingClass);
        }

        [HttpDelete("{classId}")]
        public async Task<IActionResult> DeleteClass(string classId)
        {
            var @class = await _context.Classes.FindAsync(classId);
            if (@class == null)
            {
                return NotFound();
            }

            // 删除班级前，先删除该班级的所有学生
            var students = await _context.Students
                .Where(s => s.Cid == classId)
                .ToListAsync();
            
            _context.Students.RemoveRange(students);
            _context.Classes.Remove(@class);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "班级及关联学生已删除" });
        }

        [HttpPut("{classId}/assign-teacher/{tid}")]
        public async Task<IActionResult> AssignTeacher(string classId, string tid)
        {
            var @class = await _context.Classes.FindAsync(classId);
            if (@class == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers.FindAsync(tid);
            if (teacher == null)
            {
                return BadRequest("教师不存在");
            }

            @class.Tid = tid;
            await _context.SaveChangesAsync();
            return Ok(@class);
        }

        [HttpGet("{classId}/student-count")]
        public async Task<IActionResult> GetStudentCount(string classId)
        {
            var count = await _context.Students
                .CountAsync(s => s.Cid == classId);
            return Ok(new { classId, count });
        }
    }
}
