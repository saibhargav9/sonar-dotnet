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
    public class WeakSslTlsProtocolsTest
    {
        [TestMethod]
        public void WeakSslTlsProtocols_CSharp() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\WeakSslTlsProtocols.cs",
                                    new CS.WeakSslTlsProtocols(),
                                    ParseOptionsHelper.FromCSharp8,
                                    GetAdditionalReferences());

        [TestMethod]
        public void WeakSslTlsProtocols_VB() =>
            OldVerifier.VerifyAnalyzer(@"TestCases\WeakSslTlsProtocols.vb",
                                    new VB.WeakSslTlsProtocols(),
                                    GetAdditionalReferences());

        private static IEnumerable<MetadataReference> GetAdditionalReferences() =>
           MetadataReferenceFacade.SystemNetHttp.Concat(MetadataReferenceFacade.SystemSecurityCryptography);
    }
}
