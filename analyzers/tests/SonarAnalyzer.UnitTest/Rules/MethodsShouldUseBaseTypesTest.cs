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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class MethodsShouldUseBaseTypesTest
    {
        private readonly VerifierBuilder builder = new VerifierBuilder<MethodsShouldUseBaseTypes>();

        [TestMethod]
        public void MethodsShouldUseBaseTypes_Internals()
        {
            const string code1 = @"
internal interface IFoo
{
    bool IsFoo { get; }
}

public class Foo : IFoo
{
    public bool IsFoo { get; set; }
}";
            const string code2 = @"
internal class Bar
{
    public void MethodOne(Foo foo)
    {
        var x = foo.IsFoo;
    }
}";
            var solution = SolutionBuilder.Create()
                .AddProject(AnalyzerLanguage.CSharp)
                .AddSnippet(code1)
                .Solution
                .AddProject(AnalyzerLanguage.CSharp)
                .AddProjectReference(sln => sln.ProjectIds[0])
                .AddSnippet(code2)
                .Solution;
            foreach (var compilation in solution.Compile())
            {
                DiagnosticVerifier.Verify(compilation, new MethodsShouldUseBaseTypes(), CompilationErrorBehavior.FailTest);
            }
        }

        [TestMethod]
        public void MethodsShouldUseBaseTypes() =>
            builder.AddPaths("MethodsShouldUseBaseTypes.cs", "MethodsShouldUseBaseTypes2.cs").WithAutogenerateConcurrentFiles(false).Verify();

#if NET
        [TestMethod]
        public void MethodsShouldUseBaseTypes_CSharp9() =>
            builder.AddPaths("MethodsShouldUseBaseTypes.CSharp9.cs").WithTopLevelStatements().Verify();
#endif

        [TestMethod]
        public void MethodsShouldUseBaseTypes_InvalidCode() =>
            builder.AddSnippet(@"
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Foo
{
    private void FooBar(IList<int> , IList<string>)
    {
        a.ToList();
    }

    // New test case - code doesn't compile but was making analyzer crash
    private void Foo(IList<int> a, IList<string> a)
    {
        a.ToList();
    }
}").WithErrorBehavior(CompilationErrorBehavior.Ignore).Verify();
    }
}
