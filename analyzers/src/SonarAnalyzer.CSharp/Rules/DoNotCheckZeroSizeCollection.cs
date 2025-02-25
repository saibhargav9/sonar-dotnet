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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;
using Comparison = SonarAnalyzer.Helpers.ComparisonKind;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DoNotCheckZeroSizeCollection : DoNotCheckZeroSizeCollectionBase<SyntaxKind>
    {
        protected override ILanguageFacade<SyntaxKind> Language => CSharpFacade.Instance;
        protected override string IEnumerableTString => "IEnumerable<T>";

        protected override void Initialize(SonarAnalysisContext context)
        {
            base.Initialize(context);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeIsPatternExpression, SyntaxKindEx.IsPatternExpression);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeSwitchExpression, SyntaxKindEx.SwitchExpression);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
            context.RegisterSyntaxNodeActionInNonGenerated(AnalyzePropertyPatternClause, SyntaxKindEx.PropertyPatternClause);
        }

        private void AnalyzePropertyPatternClause(SyntaxNodeAnalysisContext c)
        {
            var propertyPatternClause = (PropertyPatternClauseSyntaxWrapper)c.Node;

            foreach (var subPattern in propertyPatternClause.Subpatterns)
            {
                if (subPattern.NameColon != null)
                {
                    CheckPatternCondition(c, subPattern.NameColon.Name, subPattern.Pattern.SyntaxNode.RemoveParentheses());
                }
                else
                {
                    // Handle C#10 ExpressionColon
                }
            }
        }

        private void AnalyzePatterns(SyntaxNodeAnalysisContext c, ExpressionSyntax expression, SyntaxNode pattern)
        {
            var objectToPatternMap = new Dictionary<ExpressionSyntax, SyntaxNode>();
            PatternExpressionObjectToPatternMapping.MapObjectToPattern(expression, pattern, objectToPatternMap);

            foreach (var exp in objectToPatternMap.Keys)
            {
                CheckPatternCondition(c, exp, objectToPatternMap[exp]);
            }
        }

        private void AnalyzeIsPatternExpression(SyntaxNodeAnalysisContext c)
        {
            var isPatternExpression = (IsPatternExpressionSyntaxWrapper)c.Node;
            AnalyzePatterns(c, isPatternExpression.Expression, isPatternExpression.Pattern);
        }

        private void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext c)
        {
            var switchExpression = (SwitchExpressionSyntaxWrapper)c.Node;

            foreach (var arm in switchExpression.Arms)
            {
                AnalyzePatterns(c, switchExpression.GoverningExpression, arm.Pattern.SyntaxNode);
            }
        }

        private void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext c)
        {
            var switchStatement = (SwitchStatementSyntax)c.Node;
            foreach (var section in switchStatement.Sections)
            {
                foreach (var label in section.Labels)
                {
                    AnalyzePatterns(c, switchStatement.Expression, label);
                }
            }
        }

        private void CheckPatternCondition(SyntaxNodeAnalysisContext context, SyntaxNode expression, SyntaxNode pattern)
        {
            if (pattern.DescendantNodesAndSelf().FirstOrDefault(x => x.IsKind(SyntaxKindEx.RelationalPattern)) is { } relationalPatternNode
                && ((RelationalPatternSyntaxWrapper)relationalPatternNode) is var relationalPattern
                && ComparisonKind(relationalPattern.OperatorToken) is { } comparison
                && comparison != Comparison.None
                && Language.ExpressionNumericConverter.TryGetConstantIntValue(context.SemanticModel, relationalPattern.Expression, out var constant))
            {
                CheckExpression(context, relationalPattern.SyntaxNode, expression, constant, comparison);
            }

            static ComparisonKind ComparisonKind(SyntaxToken syntaxToken) =>
                syntaxToken.ValueText switch
                {
                    "<" => Comparison.LessThan,
                    "<=" => Comparison.LessThanOrEqual,
                    ">" => Comparison.GreaterThan,
                    ">=" => Comparison.GreaterThanOrEqual,
                    _ => Comparison.None,
                };
        }
    }
}
