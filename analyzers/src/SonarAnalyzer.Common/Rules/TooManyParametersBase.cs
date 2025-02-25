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
using SonarAnalyzer.Common;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class TooManyParametersBase<TSyntaxKind, TParameterListSyntax> : ParameterLoadingDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TParameterListSyntax : SyntaxNode
    {
        protected const string DiagnosticId = "S107";
        protected const string MessageFormat = "{0} has {1} parameters, which is greater than the {2} authorized.";
        private const int DefaultValueMaximum = 7;

        private readonly DiagnosticDescriptor rule;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        [RuleParameter("max", PropertyType.Integer, "Maximum authorized number of parameters", DefaultValueMaximum)]
        public int Maximum { get; set; } = DefaultValueMaximum;

        protected abstract TSyntaxKind[] SyntaxKinds { get; }
        protected abstract GeneratedCodeRecognizer GeneratedCodeRecognizer { get; }
        protected abstract string UserFriendlyNameForNode(SyntaxNode node);
        protected abstract int CountParameters(TParameterListSyntax parameterList);
        protected abstract int BaseParameterCount(SyntaxNode node);
        protected abstract bool CanBeChanged(SyntaxNode node, SemanticModel semanticModel);

        protected TooManyParametersBase(System.Resources.ResourceManager rspecResources) =>
            rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, rspecResources, isEnabledByDefault: false);

        protected override void Initialize(ParameterLoadingAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                GeneratedCodeRecognizer,
                c =>
                {
                    var parametersCount = CountParameters((TParameterListSyntax)c.Node);
                    var baseCount = BaseParameterCount(c.Node.Parent);
                    if (parametersCount - baseCount > Maximum && c.Node.Parent != null && CanBeChanged(c.Node.Parent, c.SemanticModel))
                    {
                        var valueText = baseCount == 0 ? parametersCount.ToString() : $"{parametersCount - baseCount} new";
                        c.ReportIssue(Diagnostic.Create(SupportedDiagnostics[0], c.Node.GetLocation(), UserFriendlyNameForNode(c.Node.Parent), valueText, Maximum));
                    }
                },
                SyntaxKinds);

        protected static bool VerifyCanBeChangedBySymbol(SyntaxNode node, SemanticModel semanticModel)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(node);
            var symbol = semanticModel.GetSymbolInfo(node).Symbol;
            if (declaredSymbol == null && symbol == null)
            {
                return false;
            }

            if (symbol != null)
            {
                return true;    // Not a declaration, such as Action
            }

            if (declaredSymbol.IsExtern && declaredSymbol.IsStatic && declaredSymbol.HasAttribute(KnownType.System_Runtime_InteropServices_DllImportAttribute))
            {
                return false;   // P/Invoke method is defined externally.
            }

            return declaredSymbol.GetOverriddenMember() == null && declaredSymbol.GetInterfaceMember() == null;
        }
    }
}
