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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RedundantConditionalAroundAssignment : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S3440";
        private const string MessageFormat = "Remove this useless conditional.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(UselessConditionIfStatement, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeActionInNonGenerated(UselessConditionSwitchExpression, SyntaxKindEx.SwitchExpression);
        }

        private static void UselessConditionIfStatement(SyntaxNodeAnalysisContext c)
        {
            var ifStatement = (IfStatementSyntax)c.Node;

            if (ifStatement.Else != null
                || ifStatement.Parent is ElseClauseSyntax
                || (ifStatement.FirstAncestorOrSelf<AccessorDeclarationSyntax>()?.IsAnyKind(SyntaxKind.SetAccessorDeclaration, SyntaxKindEx.InitAccessorDeclaration) ?? false)
                || !TryGetNotEqualsCondition(ifStatement, out var condition)
                || !TryGetSingleAssignment(ifStatement, out var assignment))
            {
                return;
            }

            var expression1Condition = condition.Left?.RemoveParentheses();
            var expression2Condition = condition.Right?.RemoveParentheses();
            var expression1Assignment = assignment.Left?.RemoveParentheses();
            var expression2Assignment = assignment.Right?.RemoveParentheses();

            if (!AreMatchingExpressions(expression1Condition, expression2Condition, expression2Assignment, expression1Assignment)
                && !AreMatchingExpressions(expression1Condition, expression2Condition, expression1Assignment, expression2Assignment))
            {
                return;
            }

            if (!(c.SemanticModel.GetSymbolInfo(assignment.Left).Symbol is IPropertySymbol))
            {
                c.ReportIssue(Diagnostic.Create(Rule, condition.GetLocation()));
            }
        }

        private static void UselessConditionSwitchExpression(SyntaxNodeAnalysisContext c)
        {
            var switchExpression = (SwitchExpressionSyntaxWrapper)c.Node;

            if (!(switchExpression.SyntaxNode.GetFirstNonParenthesizedParent() is AssignmentExpressionSyntax))
            {
                return;
            }

            foreach (var switchArm in switchExpression.Arms)
            {
                var condition = switchArm.Pattern.SyntaxNode;
                var constantPattern = condition.DescendantNodesAndSelf().FirstOrDefault(x => x.IsKind(SyntaxKindEx.ConstantPattern));
                var expression = switchArm.Expression;
                if ((constantPattern != null
                    && !(condition.IsKind(SyntaxKindEx.NotPattern) && (switchArm.WhenClause.SyntaxNode != null || switchExpression.Arms.Count != 1))
                    && CSharpEquivalenceChecker.AreEquivalent(expression, ((ConstantPatternSyntaxWrapper)constantPattern).Expression))
                    || (condition.IsKind(SyntaxKindEx.DiscardPattern) && switchExpression.Arms.Count == 1))
                {
                    c.ReportIssue(Diagnostic.Create(Rule, condition.GetLocation()));
                }
            }
        }

        private static bool TryGetNotEqualsCondition(IfStatementSyntax ifStatement, out BinaryExpressionSyntax condition)
        {
            condition = ifStatement.Condition?.RemoveParentheses() as BinaryExpressionSyntax;
            return condition != null && condition.IsKind(SyntaxKind.NotEqualsExpression);
        }

        private static bool TryGetSingleAssignment(IfStatementSyntax ifStatement, out AssignmentExpressionSyntax assignment)
        {
            var statement = ifStatement.Statement;

            if (!(statement is BlockSyntax block) || block.Statements.Count != 1)
            {
                assignment = null;
                return false;
            }

            statement = block.Statements.First();
            assignment = (statement as ExpressionStatementSyntax)?.Expression as AssignmentExpressionSyntax;

            return assignment != null && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression);
        }

        private static bool AreMatchingExpressions(SyntaxNode condition1, SyntaxNode condition2, SyntaxNode assignment1, SyntaxNode assignment2) =>
            CSharpEquivalenceChecker.AreEquivalent(condition1, assignment1) && CSharpEquivalenceChecker.AreEquivalent(condition2, assignment2);
    }
}
