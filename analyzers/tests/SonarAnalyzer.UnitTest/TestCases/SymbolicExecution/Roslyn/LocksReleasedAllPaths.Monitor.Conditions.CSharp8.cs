﻿using System;
using System.Threading;

class Program
{
    private object obj = new object();
    private object other = new object();

    private bool condition;

    public void Method1()
    {
        var lockObj = condition switch
        {
            true => obj,
            false => other,
        };

        Monitor.Enter(lockObj); // FN
        if (condition)
        {
            Monitor.Exit(lockObj);
        }
    }

    public void Method2()
    {
        var lockObj = condition switch
        {
            true => obj,
            false => other,
        };

        Monitor.Enter(lockObj); // Compliant
        if (!condition)
        {
            Monitor.Exit(other);
        }
    }
}
