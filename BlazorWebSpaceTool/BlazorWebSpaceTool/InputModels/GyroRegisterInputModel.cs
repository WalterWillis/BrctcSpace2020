using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWebSpaceTool.InputModels
{
    public class GyroRegisterInputModel
    {

        /// <summary>
        /// Amount of time in minutes to run the program
        /// </summary>
        [Required]
        [Range(0, 255, ErrorMessage = "Type a number between 0 and 255.")]
        public byte Register { get; set; }

        [Required]
        [Range(-32768, 32767, ErrorMessage = "Invalid number on Value")]
        public int Value { get; set; }

        public short ResultValue { get; set; }
    }
}
