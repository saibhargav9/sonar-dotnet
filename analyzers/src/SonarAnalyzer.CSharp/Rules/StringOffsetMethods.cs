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

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StringOffsetMethods : SonarDiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S4635";
        private const string MessageFormat = "Replace '{0}' with the overload that accepts an offset parameter.";

        private static readonly DiagnosticDescriptor rule =
            DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        private readonly string[] methodsToCheck = new string[]
        {
            "IndexOf",
            "IndexOfAny",
            "LastIndexOf",
            "LastIndexOfAny"
        };

        protected override void Initialize(SonarAnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(analysisContext =>
                {
                    var invocationExpression = (InvocationExpressionSyntax)analysisContext.Node;
                    var semanticModel = analysisContext.SemanticModel;

                    if (IsTargetMethodInvocation(invocationExpression, semanticModel) &&
                        HasSubstringMethodInvocationChild(invocationExpression, semanticModel))
                    {
                        var memberAccess = (MemberAccessExpressionSyntax)invocationExpression.Expression;

                        analysisContext.ReportIssue(Diagnostic.Create(rule, invocationExpression.GetLocation(), memberAccess.Name));
                    }
                },
                SyntaxKind.InvocationExpression);
        }

        private bool IsTargetMethodInvocation(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel) =>
            methodsToCheck.Any(methodName => invocationExpression.IsMethodInvocation(KnownType.System_String, methodName, semanticModel));

        private bool HasSubstringMethodInvocationChild(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel) =>
            invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
            memberAccessExpression.Expression is InvocationExpressionSyntax childInvocationExpression &&
            childInvocationExpression.IsMethodInvocation(KnownType.System_String, "Substring", semanticModel);

    }
}

