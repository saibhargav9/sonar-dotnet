﻿using System;

public record struct Sample
{
    public void Method1() // FN
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method2() // FN
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method3() // FN
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method4()
    {
        Console.WriteLine("Result: 0");
    }

    public string Method5()
    {
        return "foo";
    }

    public string Method6() =>
        "foo";
}

public record struct SamplePositional(string Value)
{
    public void Method1() // FN
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }

    public void Method2() // FN
    {
        string s = "test";
        Console.WriteLine("Result: {0}", s);
    }
}
