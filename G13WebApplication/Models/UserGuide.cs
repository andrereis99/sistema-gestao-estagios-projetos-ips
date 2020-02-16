using System;
using System.Collections.Generic;

namespace G13WebApplication.Models
{
    public partial class UserGuide
    {
        public int UserGuideId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
