﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2022 SonarSource SA
 * mailto: contact AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Rules.CSharp;

namespace SonarAnalyzer.UnitTest.TestFramework.Tests
{
    [TestClass]
    public class DiagnosticVerifierTest
    {
        [TestMethod]
        public void PrimaryIssueNotExpected()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            // Secondary@+1
            if (a == a)
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage(
                "CSharp*: Unexpected primary issue on line 7, span (6,21)-(6,22) with message 'Correct one of the identical expressions on both sides of operator '=='.'." + Environment.NewLine +
                "See output to see all actual diagnostics raised on the file");
        }

        [TestMethod]
        public void SecondaryIssueNotExpected()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            if (a == a) // Noncompliant
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage(
                "CSharp*: Unexpected secondary issue on line 6, span (5,16)-(5,17) with message ''." + Environment.NewLine +
                "See output to see all actual diagnostics raised on the file");
        }

        [TestMethod]
        public void UnexpectedSecondaryIssueWrongId()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            // Secondary@+1 [myWrongId]
            if (a == a) // Noncompliant [myId]
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage(
                "CSharp*: Unexpected secondary issue [myId] on line 7, span (6,16)-(6,17) with message ''." + Environment.NewLine +
                "See output to see all actual diagnostics raised on the file");
        }

        [TestMethod]
        public void SecondaryIssueUnexpectedMessage()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            // Secondary@+1 {{Wrong message}}
            if (a == a) // Noncompliant
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>().WithMessage(
                @"CSharp*: Expected secondary message on line 7 does not match actual message." + Environment.NewLine +
                "Expected: 'Wrong message'" + Environment.NewLine +
                "Actual  : ''");
        }

        [TestMethod]
        public void SecondaryIssueUnexpectedStartPosition()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            if (a == a)
//                   ^ {{Correct one of the identical expressions on both sides of operator '=='.}}
//            ^ Secondary@-1
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>()
                  .WithMessage("CSharp*: Expected secondary issue on line 6 to start on column 14 but got column 16.");
        }

        [TestMethod]
        public void SecondaryIssueUnexpectedLength()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            if (a == a)
//                   ^ {{Correct one of the identical expressions on both sides of operator '=='.}}
//              ^^^^ Secondary@-1
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>()
                  .WithMessage("CSharp*: Expected secondary issue on line 6 to have a length of 4 but got a length of 1.");
        }

        [TestMethod]
        public void ValidVerification()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedSecondary
    {
        public void Test(bool a, bool b)
        {
            // Secondary@+1
            if (a == a) // Noncompliant
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().NotThrow<UnexpectedDiagnosticException>();
        }

        [TestMethod]
        public void BuildError()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedBuildError
{",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<UnexpectedDiagnosticException>()
                  .WithMessage("CSharp*: Unexpected build error [CS1513]: } expected on line 3");
        }

        [TestMethod]
        public void UnexpectedRemainingOpeningCurlyBrace()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedRemainingCurlyBrace
    {
        public void Test(bool a, bool b)
        {
            if (a == a) // Noncompliant {Wrong format message}
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<AssertFailedException>()
                  .WithMessage("Unexpected '{' or '}' found on line: 5. Either correctly use the '{{message}}' format or remove the curly braces on the line of the expected issue");
        }

        [TestMethod]
        public void UnexpectedRemainingClosingCurlyBrace()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class UnexpectedRemainingCurlyBrace
    {
        public void Test(bool a, bool b)
        {
            if (a == a) // Noncompliant (Another Wrong format message}
            { }
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<AssertFailedException>()
                  .WithMessage("Unexpected '{' or '}' found on line: 5. Either correctly use the '{{message}}' format or remove the curly braces on the line of the expected issue");
        }

        [TestMethod]
        public void ExpectedIssuesNotRaised()
        {
            Action action =
                () => OldVerifier.VerifyCSharpAnalyzer(@"
public class ExpectedIssuesNotRaised
    {
        public void Test(bool a, bool b) // Noncompliant [MyId0]
        {
            if (a == b) // Noncompliant
            { } // Secondary [MyId1]
        }
    }",
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<AssertFailedException>().WithMessage(
                @"CSharp*: Issue(s) expected but not raised in file(s):" + Environment.NewLine +
                "File: snippet1.cs" + Environment.NewLine +
                "Line: 4, Type: primary, Id: 'MyId0'" + Environment.NewLine +
                "Line: 6, Type: primary, Id: ''" + Environment.NewLine +
                "Line: 7, Type: secondary, Id: 'MyId1'");
        }

        [TestMethod]
        public void ExpectedIssuesNotRaised_MultipleFiles()
        {
            Action action =
                () => OldVerifier.VerifyNonConcurrentAnalyzer(new[] { @"TestCases\DiagnosticsVerifier\ExpectedIssuesNotRaised.cs", @"TestCases\DiagnosticsVerifier\ExpectedIssuesNotRaised2.cs" },
                    new BinaryOperationWithIdenticalExpressions());

            action.Should().Throw<AssertFailedException>().WithMessage(
                @"CSharp*: Issue(s) expected but not raised in file(s):" + Environment.NewLine +
                "File: ExpectedIssuesNotRaised.cs" + Environment.NewLine +
                "Line: 3, Type: primary, Id: 'MyId0'" + Environment.NewLine +
                "Line: 5, Type: primary, Id: ''" + Environment.NewLine +
                "Line: 6, Type: secondary, Id: 'MyId1'" + Environment.NewLine +
                Environment.NewLine +
                "File: ExpectedIssuesNotRaised2.cs" + Environment.NewLine +
                "Line: 3, Type: primary, Id: 'MyId0'" + Environment.NewLine +
                "Line: 5, Type: primary, Id: ''" + Environment.NewLine +
                "Line: 6, Type: secondary, Id: 'MyId1'");
        }
    }
}
