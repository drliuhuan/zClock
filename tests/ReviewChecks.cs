// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Reflection;
using zClock;

class ReviewChecks
{
    static int checks;
    static void Parse(string text, int expected)
    {
        int actual;
        bool valid = InputForm.TryGetSeconds(text, out actual);
        if (valid != (expected > 0) || actual != expected)
            throw new Exception("Parse failed: " + text + " => " + actual);
        checks++;
    }
    static void Main()
    {
        Parse("1h", 3600); Parse("12h", 43200); Parse("1H", 3600);
        Parse("1时", 3600); Parse("12时", 43200); Parse("1小时", 3600);
        Parse("5400", 5400); Parse("5400s", 5400); Parse("5400秒钟", 5400);
        Parse("90m", 5400); Parse("90分", 5400); Parse("90分钟", 5400);
        Parse("90:00", 5400); Parse("1:30:00", 5400); Parse(" 1h ", 3600);
        Parse("2147483647", int.MaxValue); Parse("596523h", 2147482800);
        Parse("35791394m", 2147483640); Parse("596523:14:07", int.MaxValue);
        Parse("71582789m", 0); Parse("596524h", 0); Parse("596523:14:08", 0);
        Parse("2147483647:0:0", 0); Parse("2147483648", 0);
        Parse(null, 0); Parse("", 0); Parse(" ", 0); Parse("0", 0);
        Parse("-1h", 0); Parse("1:-1", 0); Parse("h", 0); Parse("foo", 0);
        Parse("1:2:3:4", 0); Parse("1ms", 0);
        MethodInfo format = typeof(zClock.zClock).GetMethod("FormatRemainingTime", BindingFlags.Static | BindingFlags.NonPublic);
        int[] seconds = { 0, 59, 3600, -3599, -3600, -3661, 86400, int.MinValue };
        string[] expected = { "00:00", "00:59", "01:00:00", "-59:59", "-01:00:00", "-01:01:01", "24:00:00", "-596523:14:08" };
        for (int i = 0; i < seconds.Length; i++)
        {
            string actual = (string)format.Invoke(null, new object[] { seconds[i] });
            if (actual != expected[i]) throw new Exception("Format failed: " + seconds[i] + " => " + actual);
            checks++;
        }
        Console.WriteLine("Passed " + checks + " parsing and display checks.");
    }
}
