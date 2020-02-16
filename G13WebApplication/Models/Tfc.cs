using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Tfc
    {
        [Key]
        public int TfcId { get; set; }

        [Display(Name = "Tipo")]
        [Required]
        public String TfcType { get; set; }

        [Display(Name = "Nome")]
        [Required]
        public String Name { get; set; }

        [Display(Name = "Detalhes")]
        public String Details { get; set; }

        [Display(Name = "Empresa")]
        public String Company { get; set; }

        [Display(Name = "Localização")]
        public String Location { get; set; } = " - ";

        [Display(Name = "Ficheiro")]
        public String TfcFile { get; set; }
    }
}
