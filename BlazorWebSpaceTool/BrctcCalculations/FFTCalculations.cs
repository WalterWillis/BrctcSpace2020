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
        /// Converts an array of doubles into an array of complex numbers with imaginary parts set to 0.
        /// </summary>
        /// <param name="values">Array of double values to convert.</param>
        /// <returns>An array of complex numbers.</returns>
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
        /// Calculates the frequency domain of a one-second time window using FFT.
        /// </summary>
        /// <param name="values">One-second time window of signal values.</param>
        /// <returns>A span of complex numbers representing the frequency domain.</returns>
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
        /// Performs FFT on an array of values and returns the specified number of maximum magnitudes and their associated frequencies.
        /// </summary>
        /// <param name="values">Array of signal values to perform FFT on.</param>
        /// <param name="amount">Number of maximum magnitudes to return.</param>
        /// <returns>An IEnumerable of tuples containing the frequency index and the corresponding complex magnitude.</returns>
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
        /// Performs FFT on an array of values and returns all magnitudes and their associated frequencies.
        /// </summary>
        /// <param name="values">Array of signal values to perform FFT on.</param>
        /// <returns>An array of tuples containing the frequency index and the corresponding complex magnitude.</returns>
        public static Tuple<int, Complex>[] CalculateSalientMagnitudesOfOneSecond(double[] values)
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

            return results;
        }

        /// <summary>
        /// Performs an FFT on the passed values, and generates a CSV-formatted string of maximum magnitudes and their associated frequencies.
        /// </summary>
        /// <param name="values">Array of signal values to perform FFT on.</param>
        /// <param name="magnitudeAmount">Number of maximum magnitudes to return.</param>
        /// <param name="second">The second this data represents, to be used as the ID field (optional, default is 1).</param>
        /// <returns>CSV-formatted string of maximum magnitudes and their associated frequencies.</returns>
        public static string GetMagnitudeString(double[] values,int magnitudeAmount, int second = 1)
        {
            var results = CalculateSalientMagnitudesOfOneSecond(values, magnitudeAmount);

            string magnitudeMessage = string.Empty;

            foreach(var result in results)
            {
                magnitudeMessage += $",{result.Item1},{result.Item2.Magnitude.ToString("F8")}";
            }

            return magnitudeMessage;
        }
    }
}

/*
The `FFTCalculations` class is part of a larger system that works with signals or data that change over time. 
This class helps analyze the data by breaking it down into different frequencies, making it easier to understand the important parts of the signal.

Here's a simple explanation of the key methods in this class:

1. `ConvertToComplex`: This method changes a list of numbers (called doubles) into a list of special numbers called complex numbers. 
Complex numbers have two parts: a real part (the original number) and an imaginary part (always set to 0 here).

2. `CalculateFrequencyDomainOfOneSecond`: This method takes a short piece of signal data (one second long) and changes it from being about time to being about different frequencies. 
It returns a list of complex numbers that represent these frequencies.

3. `CalculateSalientMagnitudesOfOneSecond`: This method looks at the signal data and finds the strongest frequencies and their magnitudes (how strong they are). 
You can choose how many of the strongest frequencies you want to get. This can help you focus on the most important parts of the signal.

4. `CalculateSalientMagnitudesOfOneSecond` (another version): This method is similar to the one above, 
but it gives you all the frequencies and their magnitudes, not just the strongest ones.

5. `GetMagnitudeString`: This method turns the strongest frequencies and their magnitudes into a text format called CSV (Comma Separated Values). 
This makes it easy to save the data or use it in other programs.

In short, the `FFTCalculations` class is a helper class that lets you work with signals or data that changes over time. 
It breaks the data down into different frequencies so you can understand and analyze the important parts of the signal.
*/