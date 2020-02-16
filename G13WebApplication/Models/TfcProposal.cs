using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class TfcProposal
    {
        [Key]
        public int TfcProposalId { get; set; }

        [Display(Name = "Student Number")]
        [Required]
        public int StudentNumber { get; set; }

        public int TfcId { get; set; }
    }
}
