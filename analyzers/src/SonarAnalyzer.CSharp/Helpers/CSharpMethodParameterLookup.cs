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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SonarAnalyzer.Helpers
{
    internal class CSharpMethodParameterLookup : MethodParameterLookupBase<ArgumentSyntax>
    {
        public CSharpMethodParameterLookup(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
            : this(invocation.ArgumentList, semanticModel) { }

        public CSharpMethodParameterLookup(InvocationExpressionSyntax invocation, IMethodSymbol methodSymbol)
            : this(invocation.ArgumentList, methodSymbol) { }

        public CSharpMethodParameterLookup(ArgumentListSyntax argumentList, SemanticModel semanticModel)
            : base(argumentList?.Arguments, argumentList == null ? null : semanticModel.GetSymbolInfo(argumentList.Parent).Symbol as IMethodSymbol) { }

        public CSharpMethodParameterLookup(ArgumentListSyntax argumentList, IMethodSymbol methodSymbol)
            : base(argumentList?.Arguments, methodSymbol) { }

        protected override SyntaxNode Expression(ArgumentSyntax argument) =>
            argument.Expression;

        protected override SyntaxToken? GetNameColonArgumentIdentifier(ArgumentSyntax argument) =>
            argument.NameColon?.Name.Identifier;
    }
}
