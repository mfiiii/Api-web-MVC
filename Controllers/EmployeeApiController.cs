using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using myappmvc.Data;
using myappmvc.Models;
using Microsoft.EntityFrameworkCore;
using myappmvc.DTOs;
using Microsoft.AspNetCore.OData.Query;
using log4net;
using myappmvc.Interfaces;

namespace myappmvc.Controllers
{
    [Route("odata/[controller]")]
    [ApiController]
    public class EmployeeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILog _logger;
        private readonly IRedisCacheService _cache;



        public EmployeeApiController(ApplicationDbContext context, IRedisCacheService cache)
        {
            _context = context;
            _cache = cache;
            _logger = LogManager.GetLogger(typeof(EmployeeApiController)); // Log4Net logger
        }
      

        [EnableQuery]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDTO>>> GetEmployees()
        {
            _logger.Info("Get all employees");

            try
            {
                const string cacheKey = "employee_list";

                var cached = await _cache.GetAsync<List<EmployeeDTO>>(cacheKey);
                if (cached != null)
                {
                    _logger.Info("Returned employees from cache.");
                    return Ok(cached);
                }



                var employees = await _context.Employees
                    .Include(e => e.Department)
                    .Select(e => new EmployeeDTO
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Salary = e.Salary,
                        Position = e.Position,
                        Department = e.Department != null ? new DepartmentDTO
                        {
                            Id = e.Department.Id,
                            Name = e.Department.Name
                        } : null
                    })
                    .ToListAsync();
                await _cache.SetAsync(cacheKey, employees, TimeSpan.FromMinutes(10));

                _logger.Info($"Returned {employees.Count} employees.");
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting employees", ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployee(int id)
        {
            _logger.Info($"Get employee with ID {id}");

            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (employee == null)
                {
                    _logger.Warn($"Employee with ID {id} not found");
                    return NotFound();
                }

                var dto = new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Salary = employee.Salary,
                    Position = employee.Position,
                    Department = employee.Department != null ? new DepartmentDTO
                    {
                        Id = employee.Department.Id,
                        Name = employee.Department.Name
                    } : null
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting employee with ID {id}", ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDTO>> CreateEmployee([FromBody] CreateEmployeeDTO dto)
        {
            _logger.Info("Creating a new employee");

            if (dto == null)
            {
                _logger.Warn("Received null DTO");
                return BadRequest(new { message = "Invalid data." });
            }

            try
            {
                Department? department = null;

                if (dto.Department != null)
                {
                    if (dto.Department.Id > 0)
                    {
                        department = await _context.Departments.FindAsync(dto.Department.Id);
                        if (department == null)
                        {
                            _logger.Warn($"Department ID {dto.Department.Id} not found");
                            return BadRequest(new { message = "Department not found." });
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(dto.Department.Name))
                    {
                        department = new Department { Name = dto.Department.Name };
                        _context.Departments.Add(department);
                        await _context.SaveChangesAsync();
                    }
                }

                var employee = new Employee
                {
                    Name = dto.Name,
                    Position = dto.Position,
                    Salary = dto.Salary,
                    Department = department
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("employee_list");
                _logger.Info("Employee list cache removed from Redis after deletion.");


                _logger.Info($"Employee created with ID {employee.Id}");

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Position = employee.Position,
                    Salary = employee.Salary,
                    Department = department != null ?
                        new DepartmentDTO { Id = department.Id, Name = department.Name } : null
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error creating employee", ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] CreateEmployeeDTO dto)
        {
            _logger.Info($"Updating employee with ID {id}");

            if (dto == null)
            {
                _logger.Warn("Received null DTO for UpdateEmployee");
                return BadRequest(new { message = "Invalid data." });
            }

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.Warn($"Employee with ID {id} not found");
                    return NotFound();
                }

                Department? department = null;
                if (dto.Department != null)
                {
                    if (dto.Department.Id > 0)
                    {
                        department = await _context.Departments.FindAsync(dto.Department.Id);
                        if (department == null)
                        {
                            _logger.Warn($"Department ID {dto.Department.Id} not found");
                            return BadRequest(new { message = "Department not found." });
                        }
                    }
                    else
                    {
                        department = new Department { Name = dto.Department.Name };
                        _context.Departments.Add(department);
                        await _context.SaveChangesAsync();
                    }
                }

                employee.Name = dto.Name ?? employee.Name;
                employee.Position = dto.Position ?? employee.Position;
                employee.Salary = dto.Salary != 0 ? dto.Salary : employee.Salary;
                employee.Department = department ?? employee.Department;

                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("employee_list");



                _logger.Info($"Employee with ID {id} updated");

                return Ok(new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Position = employee.Position,
                    Salary = employee.Salary,
                    Department = employee.Department != null ?
                        new DepartmentDTO { Id = employee.Department.Id, Name = employee.Department.Name } : null
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error updating employee with ID {id}", ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            _logger.Info($"Deleting employee with ID {id}");

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    _logger.Warn($"Employee with ID {id} not found");
                    return NotFound();
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync("employee_list");

                _logger.Info($"Employee with ID {id} deleted");

                return Ok(new EmployeeDTO
                {
                    Id = employee.Id,
                    Name = employee.Name,
                    Position = employee.Position,
                    Salary = employee.Salary
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error deleting employee with ID {id}", ex);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
