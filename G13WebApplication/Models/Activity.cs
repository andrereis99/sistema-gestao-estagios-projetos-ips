using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Display(Name = "Tipo de Atividade")]
        [Required]
        [StringLength(40, MinimumLength = 2)]
        public String ActivityType { get; set; }

        [Display(Name = "Data/Hora")]
        [Required]
        public DateTime DateT { get; set; }

        [Display(Name = "Local")]
        [Required]
        [StringLength(40, MinimumLength = 2)]
        public String Local { get; set; }

        [Display(Name = "Cancelado")]
        [Required]
        public int Canceled { get; set; } = 0;
    }
}
