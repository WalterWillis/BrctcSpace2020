using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWebSpaceTool.InputModels
{
    public class FullSystemInputModel
    {
        /// <summary>
        /// Amount of time in minutes to run the program
        /// </summary>
        [Required]
        [Range(.5, 20, ErrorMessage = "Type a number between 0.5 and 20.")]
        public double Minutes { get; set; } = 1;

        /// <summary>
        /// Indicates if the program has run and can begin feeding data
        /// </summary>
        public bool HasRun { get; set; } = false;

        public bool IsAccelerometerSelected { get; set; } // controls some UI functions

        public SingleDeviceInputModel AccelerometerInputModel { get; set; } = new SingleDeviceInputModel();

        public SingleDeviceInputModel GyroscopeInputModel { get; set; } = new SingleDeviceInputModel();
    }
}
