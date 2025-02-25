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
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.UnitTest.Helpers
{
    [TestClass]
    public class AnalysisRunContextTest
    {
        [TestMethod]
        public void AnalysisRunContext_WhenSyntaxTreeIsNull_ReturnsNull()
        {
            // Arrange
            var ctx = new AnalysisRunContext(null, null);

            // Act & Assert
            ctx.SyntaxTree.Should().BeNull();
        }

        [TestMethod]
        public void AnalysisRunContext_ReturnsSameSyntaxTree()
        {
            // Arrange
            var treeMock = new Mock<SyntaxTree>();
            var ctx = new AnalysisRunContext(treeMock.Object, null);

            // Act & Assert
            ctx.SyntaxTree.Should().Be(treeMock.Object);
        }

        [TestMethod]
        public void AnalysisRunContext_WhenSupportedDiagnosticsIsNull_ReturnsEmptyCollection()
        {
            // Arrange
            var ctx = new AnalysisRunContext(null, null);

            // Act & Assert
            ctx.SupportedDiagnostics.Should().NotBeNull();
            ctx.SupportedDiagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public void AnalysisRunContext_ReturnsSameCollectionOfDiagnosticDescriptors()
        {
            // Arrange
            var collection = new List<DiagnosticDescriptor>
            {
                new DiagnosticDescriptor("id", "title", "message", "category", DiagnosticSeverity.Error, false)
            };
            var ctx = new AnalysisRunContext(null, collection);

            // Act & Assert
            ctx.SupportedDiagnostics.Should().Equal(collection);
        }
    }
}
