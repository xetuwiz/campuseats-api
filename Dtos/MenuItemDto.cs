using System.ComponentModel.DataAnnotations;

namespace CampusEats.Api.Dtos;

// output shape — the public contract
public record MenuItemDto(

    int Id, string Name, decimal Price,
    string Category, bool Available

);