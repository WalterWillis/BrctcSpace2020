using BrctcCalculations;
using BrctcSpaceLibrary.DataModels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace BrctcSpaceLibrary.Processes
{
    public class AccelerometerDataAnalysis
    {
        private const int _magnitudeCount = 20;
        private const double resRatio = 5D / 4095;

        public int MagnitudeCount { get => _magnitudeCount; }

        private List<double> X_Values { get; set; } = new List<double>();
        private List<double> Y_Values { get; set; } = new List<double>();
        private List<double> Z_Values { get; set; } = new List<double>();

        /// <summary>
        /// Get the amount of samples of this analysis
        /// </summary>
        public int SampleSize { get => X_Values.Count; }

        public string X_Magnitudes { get; set; }
        public string Y_Magnitudes { get; set; }
        public string Z_Magnitudes { get; set; }

        /// <summary>
        /// Extract data from the span and add it to an internal list
        /// </summary>
        /// <param name="data"></param>
        public void ProcessData(Span<byte> accelSegment)
        {
            //convert accelSegment into an array of ints
            int[] acelValues = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(accelSegment).ToArray();
            
            //scale values
            X_Values.Add(acelValues[0] * resRatio);
            Y_Values.Add(acelValues[1] * resRatio);
            Z_Values.Add(acelValues[2] * resRatio);
        }

        /// <summary>
        /// Processes the full list using an FFT
        /// </summary>
        public void PerformFFTAnalysis()
        {
            X_Magnitudes = FFTCalculations.GetMagnitudeString(X_Values.ToArray(), _magnitudeCount);
            Y_Magnitudes = FFTCalculations.GetMagnitudeString(Y_Values.ToArray(), _magnitudeCount);
            Z_Magnitudes = FFTCalculations.GetMagnitudeString(Z_Values.ToArray(), _magnitudeCount);
        }

        public Tuple<int, Complex>[] PerformFFTAnalysis_XOnly()
        {
            return FFTCalculations.CalculateSalientMagnitudesOfOneSecond(X_Values.ToArray());
        }

        public Tuple<int, Complex>[] PerformFFTAnalysis_YOnly()
        {
            return FFTCalculations.CalculateSalientMagnitudesOfOneSecond(Y_Values.ToArray());
        }

        public Tuple<int, Complex>[] PerformFFTAnalysis_ZOnly()
        {
            return FFTCalculations.CalculateSalientMagnitudesOfOneSecond(Z_Values.ToArray());
        }

        /// <summary>
        /// Returns a string with the variable peak headers for each axis in the order of X, Y, Z
        /// </summary>
        /// <returns></returns>
        public string GenerateCsvHeaders()
        {
            string header_X = string.Empty;
            string header_Y = string.Empty;
            string header_Z = string.Empty;

            for (int i = 1; i <= _magnitudeCount; i++)
            {
                header_X += $",X_Freq{i},X_Peak{i}";
                header_Y += $",Y_Freq{i},Y_Peak{i}";
                header_Z += $",Z_Freq{i},Z_Peak{i}";
            }

            return header_X + header_Y + header_Z;
        }

        public void Reset()
        {
            X_Values.Clear();
            Y_Values.Clear();
            Z_Values.Clear();

            X_Magnitudes = string.Empty;
            Y_Magnitudes = string.Empty;
            Z_Magnitudes = string.Empty;
        }
    }
}

/*
 The `AccelerometerDataAnalysis` class is responsible for analyzing accelerometer data. 
This class processes the input data, performs Fast Fourier Transform (FFT) analysis on it, and returns the results in different formats. 

The main components of this class are:

1. Private properties for storing X, Y, and Z axis data (`X_Values`, `Y_Values`, `Z_Values`).
2. Public properties to access the magnitude count, sample size, and magnitudes for each axis (`MagnitudeCount`, `SampleSize`, `X_Magnitudes`, `Y_Magnitudes`, `Z_Magnitudes`).

The key methods of this class are:

1. `ProcessData()`: This method takes a `Span<byte>` of accelerometer data, converts it into an array of integers, scales the values using a predefined ratio, and adds the resulting data to the X, Y, and Z value lists.

2. `PerformFFTAnalysis()`: This method performs FFT analysis on the X, Y, and Z value lists, and stores the magnitudes as strings in the `X_Magnitudes`, `Y_Magnitudes`, and `Z_Magnitudes` properties.

3. `PerformFFTAnalysis_XOnly()`, `PerformFFTAnalysis_YOnly()`, `PerformFFTAnalysis_ZOnly()`: These methods perform FFT analysis on individual axes (X, Y, or Z) and return an array of Tuples containing frequency and complex values.

4. `GenerateCsvHeaders()`: This method generates a CSV header string for each axis with frequency and peak headers.

5. `Reset()`: This method resets the internal state of the object, clearing all the values and magnitudes for each axis.

Overall, this class is used for processing and analyzing accelerometer data, making it easier to perform frequency analysis and generate useful outputs for further analysis or visualization.
*/