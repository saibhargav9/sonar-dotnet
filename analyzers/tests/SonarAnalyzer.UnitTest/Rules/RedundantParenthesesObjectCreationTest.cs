﻿/*
 * SonarAnalyzer for .NET
 * Copyright (C) 2015-2021 SonarSource SA
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
using SonarAnalyzer.Rules.CSharp;
using SonarAnalyzer.UnitTest.TestFramework;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class RedundantParenthesesObjectCreationTest
    {
        [TestMethod]
        public void RedundantParenthesesObjectCreation() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\RedundantParenthesesObjectCreation.cs", new RedundantParenthesesObjectsCreation());

#if NET
        [TestMethod]
        public void RedundantParenthesesObjectCreation_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\RedundantParenthesesObjectCreation.CSharp9.cs", new RedundantParenthesesObjectsCreation());

        [TestMethod]
        public void RedundantParenthesesObjectCreation_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\RedundantParenthesesObjectCreation.CSharp10.cs", new RedundantParenthesesObjectsCreation());

        [TestMethod]
        public void RedundantParenthesesObjectCreation_CSharpPreview() =>
            OldVerifier.VerifyAnalyzerCSharpPreviewLibrary(@"TestCases\RedundantParenthesesObjectCreation.CSharp.Preview.cs", new RedundantParenthesesObjectsCreation());
#endif

        [TestMethod]
        public void RedundantParenthesesObjectCreation_CodeFix() =>
            OldVerifier.VerifyCodeFix<RedundantParenthesesCodeFix>(
                @"TestCases\RedundantParenthesesObjectCreation.cs",
                @"TestCases\RedundantParenthesesObjectCreation.Fixed.cs",
                new RedundantParenthesesObjectsCreation());
    }
}
