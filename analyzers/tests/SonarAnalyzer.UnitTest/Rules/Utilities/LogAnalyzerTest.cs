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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarAnalyzer.Helpers;
using SonarAnalyzer.Protobuf;
using SonarAnalyzer.UnitTest.Helpers;
using SonarAnalyzer.UnitTest.TestFramework;
using CS = SonarAnalyzer.Rules.CSharp;
using VB = SonarAnalyzer.Rules.VisualBasic;

namespace SonarAnalyzer.UnitTest.Rules
{
    [TestClass]
    public class LogAnalyzerTest
    {
        private const string Root = @"TestCases\Utilities\LogAnalyzer\";

        [TestMethod]
        public void LogCompilationMessages_CS()
        {
            var testRoot = Root + nameof(LogCompilationMessages_CS);
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<LogInfo>(
                new[] { Root + "Normal.cs", Root + "Second.cs" },
                new TestLogAnalyzer_CS(testRoot),
                @$"{testRoot}\log.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                VerifyCompilationMessagesNonConcurrentRuleExecution);
        }

        [TestMethod]
        public void LogCompilationMessages_CS_Concurrent()
        {
            using var scope = new EnvironmentVariableScope(false) {EnableConcurrentAnalysis = true};
            var testRoot = Root + nameof(LogCompilationMessages_CS);
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<LogInfo>(
                new[] { Root + "Normal.cs", Root + "Second.cs" },
                new TestLogAnalyzer_CS(testRoot),
                @$"{testRoot}\log.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                VerifyCompilationMessagesConcurrentRuleExecution);
        }

        [TestMethod]
        public void LogCompilationMessages_VB()
        {
            var testRoot = Root + nameof(LogCompilationMessages_VB);
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<LogInfo>(
                new[] { Root + "Normal.vb", Root + "Second.vb" },
                new TestLogAnalyzer_VB(testRoot),
                @$"{testRoot}\log.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                VerifyCompilationMessagesNonConcurrentRuleExecution);
        }

        [TestMethod]
        public void LogAutogenerated_CS()
        {
            var testRoot = Root + nameof(LogAutogenerated_CS);
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<LogInfo>(
                new[] { Root + "Normal.cs", Root + "GeneratedByName.generated.cs", Root + "GeneratedByContent.cs" },
                new TestLogAnalyzer_CS(testRoot),
                @$"{testRoot}\log.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                VerifyGenerated);
        }

        [TestMethod]
        public void LogAutogenerated_VB()
        {
            var testRoot = Root + nameof(LogAutogenerated_VB);
            OldVerifier.VerifyNonConcurrentUtilityAnalyzer<LogInfo>(
                new[] { Root + "Normal.vb", Root + "GeneratedByName.generated.vb", Root + "GeneratedByContent.vb" },
                new TestLogAnalyzer_VB(testRoot),
                @$"{testRoot}\log.pb",
                TestHelper.CreateSonarProjectConfig(testRoot, ProjectType.Product),
                VerifyGenerated);
        }

        private static void VerifyCompilationMessagesNonConcurrentRuleExecution(IEnumerable<LogInfo> messages) =>
            VerifyCompilationMessagesBase(messages, "disabled");

        private static void VerifyCompilationMessagesConcurrentRuleExecution(IEnumerable<LogInfo> messages) =>
            VerifyCompilationMessagesBase(messages, "enabled");

        private static void VerifyCompilationMessagesBase(IEnumerable<LogInfo> messages, string expectedConcurrencyMessage)
        {
            VerifyRoslynVersion(messages);
            VerifyLanguageVersion(messages);
            VerifyConcurrentExecution(messages, expectedConcurrencyMessage);
        }

        private static void VerifyRoslynVersion(IEnumerable<LogInfo> messages)
        {
            messages.Should().NotBeEmpty();
            var versionMessage = messages.SingleOrDefault(x => x.Text.Contains("Roslyn version"));
            versionMessage.Should().NotBeNull();
            versionMessage.Severity.Should().Be(LogSeverity.Info);
            versionMessage.Text.Should().MatchRegex(@"^Roslyn version: \d+(\.\d+){3}");
            var version = new Version(versionMessage.Text.Substring(16));
            version.Should().BeGreaterThan(new Version(3, 0));  // Avoid 1.0.0.0
        }

        private static void VerifyLanguageVersion(IEnumerable<LogInfo> messages)
        {
            messages.Should().NotBeEmpty();
            var versionMessage = messages.SingleOrDefault(x => x.Text.Contains("Language version"));
            versionMessage.Should().NotBeNull();
            versionMessage.Severity.Should().Be(LogSeverity.Info);
            versionMessage.Text.Should().MatchRegex(@"^Language version: (Preview|(CSharp|VisualBasic)\d+)");
        }

        private static void VerifyConcurrentExecution(IEnumerable<LogInfo> messages, string expectedConcurrencyMessage)
        {
            messages.Should().NotBeEmpty();
            var executionState = messages.SingleOrDefault(x => x.Text.Contains("Concurrent execution: "));
            executionState.Should().NotBeNull();
            executionState.Severity.Should().Be(LogSeverity.Info);
            executionState.Text.Should().Be($"Concurrent execution: {expectedConcurrencyMessage}");
        }

        private static void VerifyGenerated(IEnumerable<LogInfo> messages)
        {
            messages.Should().NotBeEmpty();
            messages.FirstOrDefault(x => x.Text.Contains("Normal.")).Should().BeNull();

            var generatedByName = messages.SingleOrDefault(x => x.Text.Contains("GeneratedByName.generated."));
            generatedByName.Should().NotBeNull();
            generatedByName.Severity.Should().Be(LogSeverity.Debug);
            generatedByName.Text.Should().Match(@"File 'GeneratedByName.generated.*' was recognized as generated");

            var generatedByContent = messages.SingleOrDefault(x => x.Text.Contains("GeneratedByContent."));
            generatedByContent.Should().NotBeNull();
            generatedByContent.Severity.Should().Be(LogSeverity.Debug);
            generatedByContent.Text.Should().Match(@"File 'GeneratedByContent.*' was recognized as generated");
        }

        // We need to set protected properties and this class exists just to enable the analyzer without bothering with additional files with parameters
        private class TestLogAnalyzer_CS : CS.LogAnalyzer
        {
            public TestLogAnalyzer_CS(string outPath)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = false;
            }
        }

        private class TestLogAnalyzer_VB : VB.LogAnalyzer
        {
            public TestLogAnalyzer_VB(string outPath)
            {
                IsAnalyzerEnabled = true;
                OutPath = outPath;
                IsTestProject = false;
            }
        }
    }
}
