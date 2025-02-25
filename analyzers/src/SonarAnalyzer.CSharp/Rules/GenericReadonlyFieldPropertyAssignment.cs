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
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GenericReadonlyFieldPropertyAssignment : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2934";
        private const string MessageFormat = "Restrict '{0}' to be a reference type or remove this assignment of '{1}'; it is useless if '{0}' is a value type.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var assignment = (AssignmentExpressionSyntax)c.Node;
                    var expression = assignment.Left;

                    ProcessPropertyChange(expression, c.SemanticModel, c);
                },
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.AddAssignmentExpression,
                SyntaxKind.SubtractAssignmentExpression,
                SyntaxKind.MultiplyAssignmentExpression,
                SyntaxKind.DivideAssignmentExpression,
                SyntaxKind.ModuloAssignmentExpression,
                SyntaxKind.AndAssignmentExpression,
                SyntaxKind.ExclusiveOrAssignmentExpression,
                SyntaxKind.OrAssignmentExpression,
                SyntaxKind.LeftShiftAssignmentExpression,
                SyntaxKind.RightShiftAssignmentExpression,
                SyntaxKindEx.CoalesceAssignmentExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                    c =>
                    {
                        var unary = (PrefixUnaryExpressionSyntax)c.Node;
                        var expression = unary.Operand;

                        ProcessPropertyChange(expression, c.SemanticModel, c);
                    },
                    SyntaxKind.PreDecrementExpression,
                    SyntaxKind.PreIncrementExpression);

            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var unary = (PostfixUnaryExpressionSyntax)c.Node;
                    var expression = unary.Operand;

                    ProcessPropertyChange(expression, c.SemanticModel, c);
                },
                SyntaxKind.PostDecrementExpression,
                SyntaxKind.PostIncrementExpression);
        }

        private static void ProcessPropertyChange(ExpressionSyntax expression, SemanticModel semanticModel, SyntaxNodeAnalysisContext context)
        {
            if (!(expression is MemberAccessExpressionSyntax memberAccess)
                || !(semanticModel.GetSymbolInfo(expression).Symbol is IPropertySymbol propertySymbol))
            {
                return;
            }

            var fieldSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol as IFieldSymbol;
            if (!IsFieldReadonlyAndPossiblyValueType(fieldSymbol)
                || IsInsideConstructorDeclaration(expression, fieldSymbol.ContainingType, semanticModel))
            {
                return;
            }

            context.ReportIssue(Diagnostic.Create(Rule, expression.GetLocation(), fieldSymbol.Name, propertySymbol.Name));
        }

        private static bool IsFieldReadonlyAndPossiblyValueType(IFieldSymbol fieldSymbol) =>
            fieldSymbol is { IsReadOnly: true }
            && GenericParameterMightBeValueType(fieldSymbol.Type as ITypeParameterSymbol);

        private static bool IsInsideConstructorDeclaration(ExpressionSyntax expression, INamedTypeSymbol currentType, SemanticModel semanticModel) =>
            semanticModel.GetEnclosingSymbol(expression.SpanStart) is IMethodSymbol { MethodKind: MethodKind.Constructor } constructorSymbol
            && constructorSymbol.ContainingType.Equals(currentType);

        private static bool GenericParameterMightBeValueType(ITypeParameterSymbol typeParameterSymbol)
        {
            if (typeParameterSymbol == null
                || typeParameterSymbol.HasReferenceTypeConstraint
                || typeParameterSymbol.HasValueTypeConstraint
                || typeParameterSymbol.ConstraintTypes.OfType<IErrorTypeSymbol>().Any())
            {
                return false;
            }

            return typeParameterSymbol.ConstraintTypes
                                      .Select(MightBeValueType)
                                      .All(basedOnPossiblyValueType => basedOnPossiblyValueType);
        }

        private static bool MightBeValueType(ITypeSymbol type) =>
            type.IsInterface()
            || GenericParameterMightBeValueType(type as ITypeParameterSymbol);
    }
}
