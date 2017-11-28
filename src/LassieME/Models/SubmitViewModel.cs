using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LassieME.Models
{
    public class SubmitViewModel
    {
        [Required]
        public string GBkey { get; set; }
        [Required]
        public bool StoreKey { get; set; }
    }
}
