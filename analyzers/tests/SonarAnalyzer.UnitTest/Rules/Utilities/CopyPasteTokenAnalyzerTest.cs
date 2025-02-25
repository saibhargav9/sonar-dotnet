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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.Rules;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class CopyPasteTokenAnalyzerTest
    {
        private const string Root = @"TestCases\Utilities\CopyPasteTokenAnalyzer\";

        public TestContext TestContext { get; set; } // Set automatically by MsTest

        [TestMethod]
        public void Verify_Unique_CS() =>
            Verify("Unique.cs", info =>
            {
                info.Should().HaveCount(102);
                info.Count(x => x.TokenValue == "$str").Should().Be(9);
                info.Count(x => x.TokenValue == "$num").Should().Be(1);
                info.Count(x => x.TokenValue == "$char").Should().Be(2);
            });

        [TestMethod]
        public void Verify_Unique_VB() =>
            Verify("Unique.vb", info =>
            {
                info.Should().HaveCount(88);
                info.Where(x => x.TokenValue == "$str").Should().HaveCount(3);
                info.Where(x => x.TokenValue == "$num").Should().HaveCount(7);
                info.Should().ContainSingle(x => x.TokenValue == "$char");
            });

        [TestMethod]
        public void Verify_Duplicated_CS() =>
            Verify("Duplicated.cs", info =>
            {
                info.Should().HaveCount(39);
                info.Where(x => x.TokenValue == "$num").Should().HaveCount(2);
            });

        [TestMethod]
        public void Verify_Duplicated_CS_GlobalUsings()
        {
            var testRoot = Root + TestContext.TestName;
            var fileName = "Duplicated.CSharp10.cs";
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<CopyPasteTokenInfo>(
                new[] { Root + fileName },
                new TestCopyPasteTokenAnalyzer_CS(testRoot, false),
                @$"{testRoot}\token-cpd.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                messages =>
                {
                    messages.Should().HaveCount(1);
                    var info = messages.Single();
                    info.FilePath.Should().Be(fileName);
                    info.TokenInfo.Should().HaveCount(39);
                    info.TokenInfo.Where(x => x.TokenValue == "$num").Should().HaveCount(2);
                },
                options: ParseOptionsHelper.FromCSharp10);
        }

        [TestMethod]
        public void Verify_DuplicatedDifferentLiterals_CS() =>
            Verify("DuplicatedDifferentLiterals.cs", info =>
            {
                info.Should().HaveCount(39);
                info.Where(x => x.TokenValue == "$num").Should().HaveCount(2);
            });

        [TestMethod]
        public void Verify_NotRunForTestProject_CS()
        {
            var testRoot = Root + TestContext.TestName;
            OldVerifier.VerifyUtilityAnalyzerIsNotRun(Root + "DuplicatedDifferentLiterals.cs",
                                                      new TestCopyPasteTokenAnalyzer_CS(testRoot, true),
                                                      @$"{testRoot}\token-cpd.pb");
        }

        private void Verify(string fileName, Action<IReadOnlyList<CopyPasteTokenInfo.Types.TokenInfo>> verifyTokenInfo)
        {
            var testRoot = Root + TestContext.TestName;
            var language = AnalyzerLanguage.FromPath(fileName);
            UtilityAnalyzerBase analyzer = language.LanguageName switch
            {
                LanguageNames.CSharp => new TestCopyPasteTokenAnalyzer_CS(testRoot, false),
                LanguageNames.VisualBasic => new TestCopyPasteTokenAnalyzer_VB(testRoot, false),
                _ => throw new UnexpectedLanguageException(language)
            };
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<CopyPasteTokenInfo>(
                new[] { Root + fileName },
                analyzer,
                @$"{testRoot}\token-cpd.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                messages =>
                {
                    messages.Should().HaveCount(1);
                    var info = messages.Single();
                    info.FilePath.Should().Be(fileName);
                    verifyTokenInfo(info.TokenInfo);
                },
                ParseOptionsHelper.Latest(language));
        }

        // We need to set protected properties and this class exists just to enable the analyzer without bothering with additional files with parameters
        private class TestCopyPasteTokenAnalyzer_CS : CS.CopyPasteTokenAnalyzer
        {
            public TestCopyPasteTokenAnalyzer_CS(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }

        private class TestCopyPasteTokenAnalyzer_VB : VB.CopyPasteTokenAnalyzer
        {
            public TestCopyPasteTokenAnalyzer_VB(string outPath, bool isTestProject)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = isTestProject;
            }
        }
    }
}
