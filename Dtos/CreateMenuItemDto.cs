using System.ComponentModel.DataAnnotations;

namespace CampusEats.Api.Dtos;

// input shape — no Id (the server assigns it)
public record CreateMenuItemDto(

    [Required, StringLength(80)] string Name,
    [Range(0, 100000)] decimal Price,
    [Required, StringLength(40)] string Category

);
