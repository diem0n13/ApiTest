using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Course.RequestsModels;

public class UserRegistrRequest
{
    [Required(ErrorMessage = "Email is required")]
    [JsonPropertyName("name")]
    [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Phone { get; set; }
}