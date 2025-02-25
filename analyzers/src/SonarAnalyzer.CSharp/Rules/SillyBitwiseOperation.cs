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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SillyBitwiseOperation : SillyBitwiseOperationBase
    {
        public SillyBitwiseOperation() : base(RspecStrings.ResourceManager) { }

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckBinary(c, -1),
                SyntaxKind.BitwiseAndExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckBinary(c, 0),
                SyntaxKind.BitwiseOrExpression,
                SyntaxKind.ExclusiveOrExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckAssignment(c, -1),
                SyntaxKind.AndAssignmentExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c => CheckAssignment(c, 0),
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression);
        }

        protected override object FindConstant(SemanticModel semanticModel, SyntaxNode node) =>
            IsFieldOrProperty(semanticModel, node, out var symbol) && !IsInSystemNamespace(symbol)
                ? null
                : node.FindConstantValue(semanticModel);

        private void CheckAssignment(SyntaxNodeAnalysisContext context, int constValueToLookFor)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (FindIntConstant(context.SemanticModel, assignment.Right) is { } constValue
                && constValue == constValueToLookFor)
            {
                var location = assignment.Parent is StatementSyntax
                    ? assignment.Parent.GetLocation()
                    : assignment.OperatorToken.CreateLocation(assignment.Right);
                context.ReportIssue(Diagnostic.Create(Rule, location));
            }
        }

        private void CheckBinary(SyntaxNodeAnalysisContext context, int constValueToLookFor)
        {
            var binary = (BinaryExpressionSyntax)context.Node;
            CheckBinary(context, binary.Left, binary.OperatorToken, binary.Right, constValueToLookFor);
        }

        private static bool IsFieldOrProperty(SemanticModel semanticModel, SyntaxNode node, out ISymbol symbol)
        {
            symbol = semanticModel.GetSymbolInfo(node).Symbol;
            return symbol is {Kind: SymbolKind.Field or SymbolKind.Property};
        }

        private static bool IsInSystemNamespace(ISymbol symbol) =>
            symbol.ContainingNamespace.Name == "System";
    }
}
