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
    public sealed class InterfacesShouldNotBeEmpty : SonarDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S4023";
        private const string MessageFormat = "Remove this interface or add members to it.";

        private static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var interfaceDeclaration = (InterfaceDeclarationSyntax)c.Node;
                    if (interfaceDeclaration.Identifier.IsMissing
                        || interfaceDeclaration.Members.Count > 0)
                    {
                        return;
                    }

                    var interfaceSymbol = c.SemanticModel.GetDeclaredSymbol(interfaceDeclaration);
                    if (interfaceSymbol is { DeclaredAccessibility: Accessibility.Public }
                        && !IsAggregatingOtherInterfaces(interfaceSymbol))
                    {
                        c.ReportIssue(Diagnostic.Create(Rule, interfaceDeclaration.Identifier.GetLocation()));
                    }
                },
                SyntaxKind.InterfaceDeclaration);

        private static bool IsAggregatingOtherInterfaces(ITypeSymbol interfaceSymbol) =>
            interfaceSymbol.AllInterfaces.Length > 1;
    }
}
