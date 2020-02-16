using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class Activity_Document
    {
        [Key]
        public int Activity_DocumentId { get; set; }

        [Display(Name = "Nome do Documento")]
        [Required]
        public String DocumentName { get; set; }

        [Display(Name = "Data de submissão")]
        [Required]
        public DateTime SubmitionData { get; set; }

        [Display(Name = "Documento")]
        [Required]
        public String DocumentPath { get; set; }

        [Display(Name = "Observações")]
        [Required]
        public String Comments { get; set; }

        [Display(Name = "Id Utilizador")]
        [Required]
        public int UserId { get; set; }

        [Display(Name = "Id Atividade")]
        [Required]
        public int ActivityId { get; set; }

        [Display(Name = "Estado")]
        [Required]
        public int FlagReject { get; set; }
    }
}