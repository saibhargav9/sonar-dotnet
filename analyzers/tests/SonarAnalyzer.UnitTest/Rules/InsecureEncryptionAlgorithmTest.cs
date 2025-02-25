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

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.UnitTest.MetadataReferences;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class InsecureEncryptionAlgorithmTest
    {
        [TestMethod]
        public void InsecureEncryptionAlgorithm_MainProject_CS() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\InsecureEncryptionAlgorithm.cs", new CS.InsecureEncryptionAlgorithm(), GetAdditionalReferences());

        [TestMethod]
        public void InsecureEncryptionAlgorithm_DoesNotRaiseIssuesForTestProject_CS() =>
            OldVerifier.VerifyNoIssueReportedInTest(@"TestCases\InsecureEncryptionAlgorithm.cs", new CS.InsecureEncryptionAlgorithm(), GetAdditionalReferences());

#if NET
        [TestMethod]
        public void InsecureEncryptionAlgorithm_CSharp9() =>
            OldVerifier.VerifyAnalyzerFromCSharp9Console(@"TestCases\InsecureEncryptionAlgorithm.CSharp9.cs", new CS.InsecureEncryptionAlgorithm(), GetAdditionalReferences());

        [TestMethod]
        public void InsecureEncryptionAlgorithm_CSharp10() =>
            OldVerifier.VerifyAnalyzerFromCSharp10Library(@"TestCases\InsecureEncryptionAlgorithm.CSharp10.cs", new CS.InsecureEncryptionAlgorithm(), GetAdditionalReferences());
#endif

        [TestMethod]
        public void InsecureEncryptionAlgorithm_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\InsecureEncryptionAlgorithm.vb", new VB.InsecureEncryptionAlgorithm(), GetAdditionalReferences());

        private static IEnumerable<MetadataReference> GetAdditionalReferences() =>
            MetadataReferenceFacade.SystemSecurityCryptography.Concat(NuGetMetadataReference.BouncyCastle());
    }
}
