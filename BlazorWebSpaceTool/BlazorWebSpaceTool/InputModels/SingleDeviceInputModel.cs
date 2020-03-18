using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace BlazorWebSpaceTool.InputModels
{
    public class SingleDeviceInputModel
    {
        /////////////////////////////////////////////////////////////////////////
        ////////Properties below are filled before program runs///////////////////
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Amount of time in minutes to run the program
        /// </summary>
        [Required]
        [Range(.5, 20, ErrorMessage = "Type a number between 0.5 and 20.")]
        public double Minutes { get; set; } = 1;


        /////////////////////////////////////////////////////////////////////////
        ////////Properties below are filled after program runs///////////////////
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// How many rows per table instance
        /// </summary>
        [Required]
        [Range(1, 2000, ErrorMessage = "Type a number between 1 and 2000.")]
        public int DataSetsPerPage { get; set; } = 500;

        /// <summary>
        /// The index of the DataSet list on the server
        /// </summary>
        [Range(1, Int32.MaxValue, ErrorMessage = "Select a page to read.")]
        public int SelectedPage { get; set; } = 1;

        /// <summary>
        /// Indicates if the program has run and can begin feeding data
        /// </summary>
        public bool HasRun { get; set; } = false;

        /// <summary>
        /// The amount of datasets resulting from running the program
        /// </summary>
        public long DataSets { get; set; } = 0;

        /// <summary>
        /// The size of the buffer that will be used to read the binary format of the file
        /// </summary>
        public int SegmentSize { get; set; } = 0;

        /// <summary>
        /// Used to keep track of the option used to generate the data seperate from the UI, like state
        /// </summary>
        public bool RunAccelerometer { get; set; }

        /// <summary>
        /// Used to keep track of the option used to generate the data seperate from the UI, like state
        /// </summary>
        public bool RunGyroscope { get; set; }

        /// <summary>
        /// The index of the currently selected page
        /// </summary>
        public long PageStart { get => ((SelectedPage * DataSetsPerPage) - DataSetsPerPage) >= 0 ? ((SelectedPage * DataSetsPerPage) - DataSetsPerPage) : 0; }

        /// <summary>
        /// Total amount of pages given the amount of datasets per page
        /// </summary>
        public int AmountOfPages { get => GetAmountOfPages(); }

        /// <summary>
        /// Keep above 0 and below a ridiculous amount
        /// </summary>
        private int GetAmountOfPages()
        {
            int pageOptions = 0;
            long pages = (DataSets / DataSetsPerPage);
            long remainder = (DataSets % DataSetsPerPage);

            if (remainder > 0)
                pages++;

            if (pages > 0)
            {
                if (pages > 2000)
                    pageOptions = 2000; // If we need any more than this, then either our row count is too small or the file is too large to view on the web and require more robust analysis
                else
                    pageOptions = (int)pages;
            }

            return pageOptions;
        }
    }
}
