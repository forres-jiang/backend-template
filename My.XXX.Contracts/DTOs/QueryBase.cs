using System.ComponentModel.DataAnnotations;

namespace My.XXX.Contracts.DTOs;

/// <summary>Raw HTTP pagination input. Application boundaries normalize it explicitly.</summary>
public class QueryBase
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PageSize { get; set; }
    [Required]
    public int PageIndex { get; set; }
}
