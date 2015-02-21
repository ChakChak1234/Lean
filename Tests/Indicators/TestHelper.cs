/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;


namespace QuantConnect.Tests.Indicators
{
    /// <summary>
    /// Provides helper methods for testing indicatora
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// Gets the trade bar data from the specified file in the TestData directory
        /// </summary>
        /// <param name="filename">The file to read</param>
        /// <returns>A stream of TradeBars parsed from the file</returns>
        public static IEnumerable<TradeBar> GetDataStream(string filename)
        {
            return File.ReadLines(Path.Combine("TestData", filename)).Skip(1).Select(line => line.Split(',')).Select(parts => new TradeBar
            {
                Time = Time.ParseDate(parts[0]),
                Open = parts[1].ToDecimal(),
                High = parts[2].ToDecimal(),
                Low = parts[3].ToDecimal(),
                Close = parts[4].ToDecimal(),
                Volume = (long) double.Parse(parts[5])
            });
        }

        /// <summary>
        /// Gets a stream of IndicatorDataPoints that can be fed to an indicator. The data stream starts at {DateTime.Today, 1m} and
        /// increasing at {1 second, 1m}
        /// </summary>
        /// <param name="count">The number of data points to stream</param>
        /// <param name="valueProducer">Function to produce the value of the data, null to use the index</param>
        /// <returns>A stream of IndicatorDataPoints</returns>
        public static IEnumerable<IndicatorDataPoint> GetDataStream(int count, Func<int, decimal> valueProducer = null)
        {
            var reference = DateTime.Today;
            valueProducer = valueProducer ?? (x => x);
            for (int i = 0; i < count; i++)
            {
                yield return new IndicatorDataPoint(reference.AddSeconds(i), valueProducer.Invoke(i));
            }
        }

        /// <summary>
        /// Compare the specified indicator against external data using the spy_with_indicators.txt file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator(IndicatorBase<IndicatorDataPoint> indicator, string targetColumn, double epsilon = 1e-3)
        {
            TestIndicator(indicator, "spy_with_indicators.txt", targetColumn, (i, expected) => Assert.AreEqual(expected, (double) i.Current.Value, epsilon));
        }

        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="customAssertion">Sets custom assertion logic, parameter is the indicator, expected value from the file</param>
        public static void TestIndicator(IndicatorBase<IndicatorDataPoint> indicator, string externalDataFilename, string targetColumn, Action<IndicatorBase<IndicatorDataPoint>, double> customAssertion)
        {
            // assumes the Date is in the first index

            bool first = true;
            int closeIndex = -1;
            int targetIndex = -1;
            foreach (var line in File.ReadLines(Path.Combine("TestData", externalDataFilename)))
            {
                string[] parts = line.Split(new[] {','}, StringSplitOptions.None);

                if (first)
                {
                    first = false;
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Trim() == "Close")
                        {
                            closeIndex = i;
                        }
                        if (parts[i].Trim() == targetColumn)
                        {
                            targetIndex = i;
                        }
                    }
                    if (closeIndex*targetIndex < 0)
                    {
                        Assert.Fail("Didn't find one of 'Close' or '{0}' in the header: " + line, targetColumn);
                    }

                    continue;
                }

                decimal close = decimal.Parse(parts[closeIndex]);
                DateTime date = Time.ParseDate(parts[0]);

                var data = new IndicatorDataPoint(date, close);
                indicator.Update(data);

                if (!indicator.IsReady || parts[targetIndex].Trim() == string.Empty)
                {
                    continue;
                }

                double expected = double.Parse(parts[targetIndex]);
                customAssertion.Invoke(indicator, expected);
            }
        }


        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator(IndicatorBase<TradeBar> indicator, string externalDataFilename, string targetColumn, double epsilon = 1e-3)
        {
            TestIndicator(indicator, externalDataFilename, targetColumn, (i, expected) => Assert.AreEqual(expected, (double)i.Current.Value, epsilon, "Failed at " + i.Current.Time.ToString("o")));
        }

        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="selector">A function that receives the indicator as input and outputs a value to match the target column</param>
        /// <param name="epsilon">The maximum delta between expected and actual</param>
        public static void TestIndicator<T>(T indicator, string externalDataFilename, string targetColumn, Func<T, double> selector, double epsilon = 1e-3)
            where T : Indicator
        {
            TestIndicator(indicator, externalDataFilename, targetColumn, (i, expected) => Assert.AreEqual(expected, selector(indicator), epsilon, "Failed at " + i.Current.Time.ToString("o")));
        }
        /// <summary>
        /// Compare the specified indicator against external data using the specificied comma delimited text file.
        /// The 'Close' column will be fed to the indicator as input
        /// </summary>
        /// <param name="indicator">The indicator under test</param>
        /// <param name="externalDataFilename"></param>
        /// <param name="targetColumn">The column with the correct answers</param>
        /// <param name="customAssertion">Sets custom assertion logic, parameter is the indicator, expected value from the file</param>
        public static void TestIndicator(IndicatorBase<TradeBar> indicator, string externalDataFilename, string targetColumn, Action<IndicatorBase<TradeBar>, double> customAssertion)
        {
            bool first = true;
            int targetIndex = -1;
            foreach (var line in File.ReadLines(Path.Combine("TestData", externalDataFilename)))
            {
                var parts = line.Split(',');
                if (first)
                {
                    first = false;
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Trim() == targetColumn)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                    continue;
                }

                var tradebar = new TradeBar
                {
                    Time = Time.ParseDate(parts[0]),
                    Open = parts[1].ToDecimal(),
                    High = parts[2].ToDecimal(),
                    Low = parts[3].ToDecimal(),
                    Close = parts[4].ToDecimal(),
                    //Volume = long.Parse(parts[5])
                };

                indicator.Update(tradebar);

                if (!indicator.IsReady || parts[targetIndex].Trim() == string.Empty)
                {
                    continue;
                }

                double expected = double.Parse(parts[targetIndex]);
                customAssertion.Invoke(indicator, expected);
            }
        }

        /// <summary>
        /// Asserts that the indicator has zero samples, is not ready, and has the default value
        /// </summary>
        /// <param name="indicator">The indicator to assert</param>
        public static void AssertIndicatorIsInDefaultState<T>(IndicatorBase<T> indicator)
            where T : BaseData
        {
            Assert.AreEqual(0m, indicator.Current.Value);
            Assert.AreEqual(DateTime.MinValue, indicator.Current.Time);
            Assert.AreEqual(0, indicator.Samples);
            Assert.IsFalse(indicator.IsReady);
        }

        /// <summary>
        /// Gets a customAssertion action which will gaurantee that the delta between the expected and the
        /// actual continues to decrease with a lower bound as specified by the epsilon parameter.  This is useful
        /// for testing indicators which retain theoretically infinite information via methods such as exponential smoothing
        /// </summary>
        /// <param name="epsilon">The largest increase in the delta permitted</param>
        /// <returns></returns>
        public static Action<IndicatorBase<IndicatorDataPoint>, double> AssertDeltaDecreases(double epsilon)
        {
            double delta = double.MaxValue;
            return (indicator, expected) =>
            {
                // the delta should be forever decreasing
                var currentDelta = Math.Abs((double) indicator.Current.Value - expected);
                if (currentDelta - delta > epsilon)
                {
                    Assert.Fail("The delta increased!");
                    //Console.WriteLine(indicator.Value.Time.Date.ToShortDateString() + " - " + indicator.Value.Data.ToString("000.000") + " \t " + expected.ToString("000.000") + " \t " + currentDelta.ToString("0.000"));
                }
                delta = currentDelta;
            };
        }
    }
}