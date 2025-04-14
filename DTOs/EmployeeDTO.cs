using System.ComponentModel.DataAnnotations;

namespace myappmvc.DTOs
{
    public class EmployeeDTO
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Position { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Lương phải >= 0")]
        public decimal Salary { get; set; }

        public DepartmentDTO Department { get; set; } 

    }

}
