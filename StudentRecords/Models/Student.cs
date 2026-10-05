using System.ComponentModel.DataAnnotations;

namespace StudentRecords.Models;

public class Student
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Course { get; set; } = string.Empty;

    [Range(0, 100)]
    [Display(Name = "Grade")]
    public decimal Grade { get; set; }
}