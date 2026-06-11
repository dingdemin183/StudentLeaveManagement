using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentLeaveSystem.Data;
using StudentLeaveSystem.Models;

namespace StudentLeaveSystem.Controllers
{
    [Route("api/departments")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment([FromBody] Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDepartmentById), new { did = department.Did }, department);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _context.Departments.ToListAsync();
            return Ok(departments);
        }

        [HttpGet("{did}")]
        public async Task<IActionResult> GetDepartmentById(string did)
        {
            var department = await _context.Departments.FindAsync(did);
            if (department == null)
            {
                return NotFound();
            }
            return Ok(department);
        }

        [HttpPut("{did}")]
        public async Task<IActionResult> UpdateDepartment(string did, [FromBody] Department department)
        {
            if (did != department.Did)
            {
                return BadRequest();
            }

            _context.Entry(department).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(department);
        }

        [HttpDelete("{did}")]
        public async Task<IActionResult> DeleteDepartment(string did)
        {
            var department = await _context.Departments.FindAsync(did);
            if (department == null)
            {
                return NotFound();
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
