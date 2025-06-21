using System;
using System.Collections.Generic;

namespace FisFaturaAPI.Models
{
    public class ReportRequestDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
    }
} 