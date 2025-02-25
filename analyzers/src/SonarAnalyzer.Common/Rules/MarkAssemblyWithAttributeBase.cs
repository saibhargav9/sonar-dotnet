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
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class MarkAssemblyWithAttributeBase : SonarDiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor rule;

        private protected abstract KnownType AttributeToFind { get; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        protected override bool EnableConcurrentExecution => false;

        protected MarkAssemblyWithAttributeBase(DiagnosticDescriptor rule) =>
            this.rule = rule;

        protected sealed override void Initialize(SonarAnalysisContext context) =>
            context.RegisterCompilationStartAction(c =>
                c.RegisterCompilationEndAction(cc =>
                    {
                        if (!cc.Compilation.Assembly.HasAttribute(AttributeToFind) && !cc.Compilation.Assembly.HasAttribute(KnownType.Microsoft_AspNetCore_Razor_Hosting_RazorCompiledItemAttribute))
                        {
                            cc.ReportIssue(Diagnostic.Create(rule, null, cc.Compilation.AssemblyName));
                        }
                    })
                );
    }
}
