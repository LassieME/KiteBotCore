using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LassieME.Models
{
    public class ResultViewModel
    {
        [Required]
        public string SuccessString { get; set; }
    }
}
