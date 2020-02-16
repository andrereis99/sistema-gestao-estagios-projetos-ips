using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace G13WebApplication.Models
{
    public class WorkPlan
    {
        [Key]
        public int PlanId { get; set; }

        [Display(Name = "Ficheiro")]
        public String PlanFile { get; set; }

        [Display(Name = "Confirmed")]
        public int Confirmed { get; set; }

        [Display(Name = "TfcId")]
        public int? TfcIdFk { get; set; }

        [Display(Name = "Tipo Tfc")]
        public String TfcType { get; set; }
    }
}
