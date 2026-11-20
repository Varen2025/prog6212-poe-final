/*
   TITLE: Switch expression - C# reference
   AUTHOR: Microsoft
   DATE: n.d.
   AVAILABLE:
   https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression
   DATE ACCESSED: 20 November 2026

   Usage: Referenced for choosing the st, nd, rd or th ending of a
   finishing position with a switch expression.
*/

using System.Globalization;

namespace RaceDay.Web.Services
{
    // Turns a finishing position into text such as
    // "47th out of 312 finishers".
    public static class PositionFormatter
    {
        // Returns 1st, 2nd, 3rd, 4th ... 11th, 12th, 13th, 21st.
        public static string Ordinal(int number)
        {
            int lastTwoDigits = number % 100;

            string suffix;

            // 11, 12 and 13 always end in "th".
            if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            {
                suffix = "th";
            }
            else
            {
                suffix = (number % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th"
                };
            }

            return number.ToString(CultureInfo.InvariantCulture) + suffix;
        }

        // Returns "2nd out of 12 finishers". If the total is not
        // known, only the position is returned.
        public static string Describe(int position, int totalFinishers)
        {
            if (totalFinishers <= 0)
            {
                return Ordinal(position);
            }

            return Ordinal(position) + " out of " +
                totalFinishers.ToString(CultureInfo.InvariantCulture) +
                (totalFinishers == 1 ? " finisher" : " finishers");
        }
    }
}