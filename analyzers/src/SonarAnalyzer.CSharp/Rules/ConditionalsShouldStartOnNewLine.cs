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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ConditionalsShouldStartOnNewLine : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3972";
        private const string MessageFormat = "Move this 'if' to a new line or add the missing 'else'.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(c =>
            {
                var ifKeyword = ((IfStatementSyntax)c.Node).IfKeyword;

                if (TryGetPreviousTokenInSameLine(ifKeyword, out var previousTokenInSameLine) &&
                    previousTokenInSameLine.IsKind(SyntaxKind.CloseBraceToken))
                {
                    c.ReportIssue(Diagnostic.Create(rule,
                            ifKeyword.GetLocation(),
                            additionalLocations: new[] { previousTokenInSameLine.GetLocation() }
                        ));
                }
            },
            SyntaxKind.IfStatement);
        }

        private static bool TryGetPreviousTokenInSameLine(SyntaxToken token, out SyntaxToken previousToken)
        {
            previousToken = token.GetPreviousToken();
            return GetFirstLineNumber(previousToken) == GetFirstLineNumber(token);
        }

        private static int GetFirstLineNumber(SyntaxToken token)
            => token.GetLocation().GetLineSpan().StartLinePosition.Line;
    }
}
