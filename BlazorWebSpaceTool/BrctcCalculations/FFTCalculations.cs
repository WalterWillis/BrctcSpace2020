using FFTW.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace BrctcCalculations
{
    public class FFTCalculations
    {
        /// <summary>
        /// Convert a list of doubles into a list of complex numbers
        /// </summary>
        /// <param name="values">Values to convert</param>
        /// <returns></returns>
        public static Complex[] ConvertToComplex(double[] values)
        {
            Complex[] data = new Complex[values.Length];

            for(int i = 0; i < values.Length; i++)
            {
                data[i] = new Complex(values[i], 0); //imaginary part should always be 0... i think
            }

            return data;
        }

        /// <summary>
        /// Calculate the frequency domain of one second of data
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Span<Complex> CalculateFrequencyDomainOfOneSecond(double[] values)
        {
            Complex[] input = ConvertToComplex(values);
            Complex[] output = new Complex[input.Length];

            using (var pinIn = new PinnedArray<Complex>(input))
            using (var pinOut = new PinnedArray<Complex>(output))
            {
                DFT.FFT(pinIn, pinOut); //leave extra threads out as we are already multithreading
            }

            //only half the data is within the frequency domain
            return output.AsSpan().Slice(0, (input.Length / 2) - 1); 
        }

        /// <summary>
        /// Performs FFT on a list of values returns a list of Frequencies associated with maximum magnitudes
        /// </summary>
        /// <param name="values">Values to perform FFT on</param>
        /// <param name="amount">Amount of maximum aplitudes to return</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<int, Complex>> CalculateSalientMagnitudesOfOneSecond(double[] values, int amount)
        {
            Complex[] input = ConvertToComplex(values);
            Complex[] output = new Complex[input.Length];

            using (var pinIn = new PinnedArray<Complex>(input))
            using (var pinOut = new PinnedArray<Complex>(output))
            {
                DFT.FFT(pinIn, pinOut); //leave extra threads out as we are already multithreading
            }

            //only half the data is within the frequency domain
            //tuples appear to be faster than dictionaries for this particular use case (sorting, returning, etc.)
            Tuple<int, Complex>[] results = new Tuple<int, Complex>[output.Length / 2];
            for (int i = 0; i < output.Length / 2; i++)
            {
                results[i] = new Tuple<int, Complex>(i, output[i] / input[i]);
            }

            IEnumerable<Tuple<int, Complex>> max = (from result in results
                       where true
                       orderby result.Item2.Magnitude descending
                       select result).Take(amount);

            return max;
        }

        /// <summary>
        /// Performs an FFT on the passed values and creates a CSV-based string
        /// </summary>
        /// <param name="values">Values to perform FFT on</param>
        /// <param name="second">The second this data represents, to be used as the ID field</param>
        /// <param name="magnitudeAmount">Amount of maximum amplitudes to return</param>
        /// <returns>CSV-formatted string of all magnitudes and their associated frequencies</returns>
        public static string GetMagnitudeString(double[] values,int magnitudeAmount, int second = 1)
        {
            var results = CalculateSalientMagnitudesOfOneSecond(values, magnitudeAmount);

            string magnitudeMessage = string.Empty;

            foreach(var result in results)
            {
                magnitudeMessage += $",{result.Item1},{result.Item2.Magnitude}";
            }

            return magnitudeMessage;
        }
    }
}
