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
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Common;
using SonarAnalyzer.Extensions;
using SonarAnalyzer.Helpers;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InsecureDeserialization : HotspotDiagnosticAnalyzer
    {
        private const string DiagnosticId = "S5766";
        private const string MessageFormat = "Make sure not performing data validation after deserialization is safe here.";

        private static readonly DiagnosticDescriptor Rule =
            DiagnosticDescriptorBuilder
                .GetDescriptor(DiagnosticId, MessageFormat, RspecStrings.ResourceManager)
                .WithNotConfigurable();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public InsecureDeserialization()
            : base(AnalyzerConfiguration.Hotspot)
        {
        }

        public InsecureDeserialization(IAnalyzerConfiguration analyzerConfiguration)
            : base(analyzerConfiguration)
        {
        }

        protected override void Initialize(SonarAnalysisContext context) =>
            context.RegisterSyntaxNodeActionInNonGenerated(VisitDeclaration, SyntaxKind.ClassDeclaration, SyntaxKindEx.RecordClassDeclaration);

        private void VisitDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!IsEnabled(context.Options) || context.ContainingSymbol.Kind != SymbolKind.NamedType)
            {
                return;
            }

            var declaration = (TypeDeclarationSyntax)context.Node;
            if (!HasConstructorsWithParameters(declaration))
            {
                // If there are no constructors, or if these don't have parameters, there is no validation done
                // and the type is considered safe.
                return;
            }

            var typeSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);
            if (!HasSerializableAttribute(typeSymbol))
            {
                return;
            }

            ReportDiagnostics(declaration, typeSymbol, context);
        }

        private static void ReportDiagnostics(TypeDeclarationSyntax declaration, ITypeSymbol typeSymbol, SyntaxNodeAnalysisContext context)
        {
            var implementsISerializable = ImplementsISerializable(typeSymbol);
            var implementsIDeserializationCallback = ImplementsIDeserializationCallback(typeSymbol);

            var walker = new ConstructorDeclarationWalker(context.SemanticModel);
            walker.SafeVisit(declaration);

            if (!implementsISerializable
                && !implementsIDeserializationCallback)
            {
                foreach (var ctorInfo in walker.GetConstructorsInfo().Where(info => info.HasConditionalConstructs))
                {
                    context.ReportIssue(Diagnostic.Create(Rule, ctorInfo.GetReportLocation()));
                }
            }

            if (implementsISerializable
                && !walker.HasDeserializationCtorWithConditionalStatements())
            {
                foreach (var ctorInfo in walker.GetConstructorsInfo().Where(info => !info.IsDeserializationConstructor && info.HasConditionalConstructs))
                {
                    context.ReportIssue(Diagnostic.Create(Rule, ctorInfo.GetReportLocation()));
                }
            }

            if (implementsIDeserializationCallback
                && !OnDeserializationHasConditions(declaration, context.SemanticModel))
            {
                foreach (var ctorInfo in walker.GetConstructorsInfo().Where(info => info.HasConditionalConstructs))
                {
                    context.ReportIssue(Diagnostic.Create(Rule, ctorInfo.GetReportLocation()));
                }
            }
        }

        private static bool OnDeserializationHasConditions(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel) =>
            typeDeclaration
                .Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(methodDeclaration => IsOnDeserialization(methodDeclaration, semanticModel))
                .ContainsConditionalConstructs();

        private static bool IsOnDeserialization(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel) =>
            methodDeclaration.Identifier.Text == "OnDeserialization"
            && methodDeclaration.ParameterList.Parameters.Count == 1
            && methodDeclaration.ParameterList.Parameters[0].IsDeclarationKnownType(KnownType.System_Object, semanticModel);

        private static bool HasConstructorsWithParameters(TypeDeclarationSyntax typeDeclaration) =>
            typeDeclaration
                .Members
                .OfType<ConstructorDeclarationSyntax>()
                .Any(constructorDeclaration => constructorDeclaration.ParameterList.Parameters.Count > 0);

        private static bool HasSerializableAttribute(ISymbol symbol) =>
            symbol.HasAttribute(KnownType.System_SerializableAttribute);

        private static bool ImplementsISerializable(ITypeSymbol symbol) =>
            symbol.Implements(KnownType.System_Runtime_Serialization_ISerializable);

        private static bool ImplementsIDeserializationCallback(ITypeSymbol symbol) =>
            symbol.Implements(KnownType.System_Runtime_Serialization_IDeserializationCallback);

        /// <summary>
        /// This walker is responsible to visit all constructor declarations and check if parameters are used in a
        /// conditional structure or not.
        /// </summary>
        private sealed class ConstructorDeclarationWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel semanticModel;
            private readonly List<ConstructorInfo> constructorsInfo = new List<ConstructorInfo>();

            private bool visitedFirstLevel;

            public ConstructorDeclarationWalker(SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
            }

            public ImmutableArray<ConstructorInfo> GetConstructorsInfo() => constructorsInfo.ToImmutableArray();

            public bool HasDeserializationCtorWithConditionalStatements() =>
                GetDeserializationConstructor() is {HasConditionalConstructs: true};

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
            {
                var isDeserializationCtor = IsDeserializationConstructor(node);

                var hasConditionalStatements = isDeserializationCtor
                    ? node.ContainsConditionalConstructs()
                    : HasParametersUsedInConditionalConstructs(node);

                constructorsInfo.Add(new ConstructorInfo(node, hasConditionalStatements, isDeserializationCtor));

                base.VisitConstructorDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (visitedFirstLevel)
                {
                    // Skip nested visits. The rule will be triggered for them also.
                    return;
                }

                visitedFirstLevel = true;
                base.VisitClassDeclaration(node);
            }

            public override void Visit(SyntaxNode node)
            {
                if (node.Kind() == SyntaxKindEx.RecordClassDeclaration)
                {
                    if (visitedFirstLevel)
                    {
                        // Skip nested visits. The rule will be triggered for them also.
                        return;
                    }

                    visitedFirstLevel = true;
                }

                base.Visit(node);
            }

            private bool HasParametersUsedInConditionalConstructs(BaseMethodDeclarationSyntax declaration)
            {
                var symbols = GetConstructorParameterSymbols(declaration, semanticModel);

                var conditionalsWalker = new ConditionalsWalker(semanticModel, symbols);
                conditionalsWalker.SafeVisit(declaration);

                return conditionalsWalker.HasParametersUsedInConditionalConstructs;
            }

            private ConstructorInfo GetDeserializationConstructor() =>
                constructorsInfo.SingleOrDefault(info => info.IsDeserializationConstructor);

            private bool IsDeserializationConstructor(BaseMethodDeclarationSyntax declaration) =>
                // A deserialization ctor has the following parameters: (SerializationInfo information, StreamingContext context)
                // See https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.iserializable?view=netcore-3.1#remarks
                declaration.ParameterList.Parameters.Count == 2
                && declaration.ParameterList.Parameters[0].IsDeclarationKnownType(KnownType.System_Runtime_Serialization_SerializationInfo, semanticModel)
                && declaration.ParameterList.Parameters[1].IsDeclarationKnownType(KnownType.System_Runtime_Serialization_StreamingContext, semanticModel);

            private static ImmutableArray<ISymbol> GetConstructorParameterSymbols(BaseMethodDeclarationSyntax node, SemanticModel semanticModel) =>
                node.ParameterList.Parameters
                    .Select(syntax => (ISymbol)semanticModel.GetDeclaredSymbol(syntax))
                    .ToImmutableArray();
        }

        /// <summary>
        /// This walker is responsible to visit all conditional structures and check if a list of parameters
        /// are used or not.
        /// </summary>
        private sealed class ConditionalsWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel semanticModel;
            private readonly ISet<string> parameterNames;

            public ConditionalsWalker(SemanticModel semanticModel, ImmutableArray<ISymbol> parameters)
            {
                this.semanticModel = semanticModel;

                parameterNames = parameters.Select(parameter => parameter.Name).ToHashSet();
            }

            public bool HasParametersUsedInConditionalConstructs { get; private set; }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                UpdateParameterValidationStatus(node.Condition);

                base.VisitIfStatement(node);
            }

            public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                UpdateParameterValidationStatus(node.Condition);

                base.VisitConditionalExpression(node);
            }

            public override void VisitSwitchStatement(SwitchStatementSyntax node)
            {
                UpdateParameterValidationStatus(node.Expression);

                base.VisitSwitchStatement(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.CoalesceExpression))
                {
                    UpdateParameterValidationStatus(node.Left);
                }

                base.VisitBinaryExpression(node);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKindEx.CoalesceAssignmentExpression))
                {
                    UpdateParameterValidationStatus(node.Left);
                }

                base.VisitAssignmentExpression(node);
            }

            public override void Visit(SyntaxNode node)
            {
                if (node.IsKind(SyntaxKindEx.SwitchExpression))
                {
                    UpdateParameterValidationStatus(((SwitchExpressionSyntaxWrapper)node).GoverningExpression);
                }

                if (node.IsKind(SyntaxKindEx.SwitchExpressionArm))
                {
                    var arm = (SwitchExpressionArmSyntaxWrapper)node;

                    if (arm.Pattern.SyntaxNode != null)
                    {
                        UpdateParameterValidationStatus(arm.Pattern);
                    }

                    if (arm.WhenClause.SyntaxNode != null)
                    {
                        UpdateParameterValidationStatus(arm.WhenClause);
                    }
                }

                base.Visit(node);
            }

            private void UpdateParameterValidationStatus(SyntaxNode node) =>
                HasParametersUsedInConditionalConstructs |= node
                    .DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Where(identifier => parameterNames.Contains(identifier.Identifier.Text))
                    .Select(identifier => semanticModel.GetSymbolInfo(identifier).Symbol)
                    .Any(symbol => symbol != null);
        }

        private sealed class ConstructorInfo
        {
            private readonly ConstructorDeclarationSyntax declarationSyntax;

            public ConstructorInfo(ConstructorDeclarationSyntax declaration,
                bool hasConditionalConstructs,
                bool isDeserializationConstructor)
            {
                declarationSyntax = declaration;
                HasConditionalConstructs = hasConditionalConstructs;
                IsDeserializationConstructor = isDeserializationConstructor;
            }

            public bool HasConditionalConstructs { get; }

            public bool IsDeserializationConstructor { get; }

            public Location GetReportLocation() =>
                declarationSyntax.Identifier.GetLocation();
        }
    }
}
