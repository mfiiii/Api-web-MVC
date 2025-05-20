namespace myappmvc.DTOs
{
    public class UpdateEmployeeDTO
    {
        public string? Name { get; set; }
        public string? Position { get; set; }
        public decimal? Salary { get; set; }  

        public DepartmentDTO? Department { get; set; }
    }
}
