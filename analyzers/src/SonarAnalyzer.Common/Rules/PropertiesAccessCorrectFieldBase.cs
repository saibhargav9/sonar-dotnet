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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarAnalyzer.Helpers;

namespace SonarAnalyzer.Rules
{
    public abstract class PropertiesAccessCorrectFieldBase : SonarDiagnosticAnalyzer
    {
        protected const string DiagnosticId = "S4275";
        private const string MessageFormat = "Refactor this {0} so that it actually refers to the field '{1}'.";

        private readonly DiagnosticDescriptor rule;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(rule);

        /**
         * Assignments can be done either
         * - directly via an assignment
         * - indirectly, when passed as 'out' or 'ref' parameter
         */
        protected abstract IEnumerable<FieldData> FindFieldAssignments(IPropertySymbol property, Compilation compilation);
        protected abstract IEnumerable<FieldData> FindFieldReads(IPropertySymbol property, Compilation compilation);
        protected abstract bool ImplementsExplicitGetterOrSetter(IPropertySymbol property);
        protected abstract bool ShouldIgnoreAccessor(IMethodSymbol accessorMethod, Compilation compilation);

        protected PropertiesAccessCorrectFieldBase(System.Resources.ResourceManager rspecResources)
        {
            rule = DiagnosticDescriptorBuilder.GetDescriptor(DiagnosticId, MessageFormat, rspecResources);
        }

        protected override void Initialize(SonarAnalysisContext context) =>
            // We want to check the fields read and assigned in all properties in this class
            // so this is a symbol-level rule (also means the callback is called only once
            // for partial classes)
            context.RegisterSymbolAction(CheckType, SymbolKind.NamedType);

        protected static SyntaxNode FindInvokedMethod(Compilation compilation, INamedTypeSymbol containingType, SyntaxNode expression) =>
            compilation.GetSemanticModel(expression.SyntaxTree) is { } semanticModel
            && semanticModel.GetSymbolInfo(expression).Symbol is { } invocationSymbol
            && invocationSymbol.ContainingType.Equals(containingType)
            && invocationSymbol.DeclaringSyntaxReferences.Length == 1
            && invocationSymbol.DeclaringSyntaxReferences.Single().GetSyntax() is { } invokedMethod
            ? invokedMethod
            : null;

        private void CheckType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (!symbol.TypeKind.Equals(TypeKind.Class)
                && !symbol.TypeKind.Equals(TypeKind.Structure))
            {
                return;
            }

            var fields = SelfAndBaseTypesFieldSymbols(symbol);
            if (!fields.Any())
            {
                return;
            }

            var properties = GetExplicitlyDeclaredProperties(symbol);
            if (!properties.Any())
            {
                return;
            }

            var propertyToFieldMatcher = new PropertyToFieldMatcher(fields);
            var allPropertyData = CollectPropertyData(properties, context.Compilation);

            // Check that if there is a single matching field name it is used by the property
            foreach (var data in allPropertyData)
            {
                var expectedField = propertyToFieldMatcher.GetSingleMatchingFieldOrNull(data.PropertySymbol);
                if (expectedField != null)
                {
                    if (!data.IgnoreGetter)
                    {
                        CheckExpectedFieldIsUsed(data.PropertySymbol.GetMethod, expectedField, data.ReadFields, context);
                    }
                    if (!data.IgnoreSetter)
                    {
                        CheckExpectedFieldIsUsed(data.PropertySymbol.SetMethod, expectedField, data.UpdatedFields, context);
                    }
                }
            }
        }

        private static IEnumerable<IFieldSymbol> SelfAndBaseTypesFieldSymbols(INamedTypeSymbol typeSymbol)
        {
            var fieldSymbols = Enumerable.Empty<IFieldSymbol>();
            var selfAndBaseTypesSymbols = typeSymbol.GetSelfAndBaseTypes();
            foreach (var symbol in selfAndBaseTypesSymbols)
            {
                fieldSymbols = fieldSymbols.Concat(symbol.GetMembers().Where(m => m.Kind.Equals(SymbolKind.Field)).OfType<IFieldSymbol>());
            }
            return fieldSymbols;
        }

        private IEnumerable<IPropertySymbol> GetExplicitlyDeclaredProperties(INamedTypeSymbol symbol) =>
            symbol.GetMembers()
                  .Where(m => m.Kind.Equals(SymbolKind.Property))
                  .OfType<IPropertySymbol>()
                  .Where(ImplementsExplicitGetterOrSetter);

        private void CheckExpectedFieldIsUsed(IMethodSymbol methodSymbol, IFieldSymbol expectedField, ImmutableArray<FieldData> actualFields, SymbolAnalysisContext context)
        {
            var expectedFieldIsUsed = actualFields.Any(a => a.Field.Equals(expectedField));
            if (!expectedFieldIsUsed || !actualFields.Any())
            {
                var locationAndAccessorType = GetLocationAndAccessor(actualFields, methodSymbol);
                if (locationAndAccessorType.Item1 != null)
                {
                    context.ReportIssue(Diagnostic.Create(
                        rule,
                        locationAndAccessorType.Item1,
                        locationAndAccessorType.Item2,
                        expectedField.Name));
                }
            }

            static Tuple<Location, string> GetLocationAndAccessor(ImmutableArray<FieldData> fields, IMethodSymbol method)
            {
                Location location;
                string accessorType;
                if (fields.Count(x => x.UseFieldLocation) == 1)
                {
                    var fieldWithValue = fields.First();
                    location = fieldWithValue.LocationNode.GetLocation();
                    accessorType = fieldWithValue.AccessorKind.Equals(AccessorKind.Getter) ? "getter" : "setter";
                }
                else
                {
                    Debug.Assert(method != null, "Method symbol should not be null at this point");
                    location = method?.Locations.First();
                    accessorType = method?.MethodKind == MethodKind.PropertyGet ? "getter" : "setter";
                }
                return Tuple.Create(location, accessorType);
            }
        }

        private IList<PropertyData> CollectPropertyData(IEnumerable<IPropertySymbol> properties, Compilation compilation)
        {
            IList<PropertyData> allPropertyData = new List<PropertyData>();

            // Collect the list of fields read/written by each property
            foreach (var property in properties)
            {
                var readFields = FindFieldReads(property, compilation);
                var updatedFields = FindFieldAssignments(property, compilation);
                var ignoreGetter = ShouldIgnoreAccessor(property.GetMethod, compilation);
                var ignoreSetter = ShouldIgnoreAccessor(property.SetMethod, compilation);
                var data = new PropertyData(property, readFields, updatedFields, ignoreGetter, ignoreSetter);
                allPropertyData.Add(data);
            }
            return allPropertyData;
        }

        private readonly struct PropertyData
        {
            public PropertyData(IPropertySymbol propertySymbol, IEnumerable<FieldData> read, IEnumerable<FieldData> updated, bool ignoreGetter, bool ignoreSetter)
            {
                PropertySymbol = propertySymbol;
                ReadFields = read.ToImmutableArray();
                UpdatedFields = updated.ToImmutableArray();
                IgnoreGetter = ignoreGetter;
                IgnoreSetter = ignoreSetter;
            }

            public IPropertySymbol PropertySymbol { get; }

            public ImmutableArray<FieldData> ReadFields { get; }

            public ImmutableArray<FieldData> UpdatedFields { get; }

            public bool IgnoreGetter { get; }

            public bool IgnoreSetter { get; }
        }

        protected enum AccessorKind
        {
            Getter,
            Setter
        }

        protected struct FieldData
        {
            public FieldData(AccessorKind accessor, IFieldSymbol field, SyntaxNode locationNode, bool useFieldLocation)
            {
                AccessorKind = accessor;
                Field = field;
                LocationNode = locationNode;
                UseFieldLocation = useFieldLocation;
            }

            public AccessorKind AccessorKind { get; }

            public IFieldSymbol Field { get; }

            public SyntaxNode LocationNode { get; }

            public bool UseFieldLocation { get; }
        }

        /// <summary>
        /// The rule decides if a property is returning/settings the expected field.
        /// We decide what the expected field name should be based on a fuzzy match
        /// between the field name and the property name.
        /// This class hides the details of matching logic.
        /// </summary>
        private class PropertyToFieldMatcher
        {
            private readonly IDictionary<IFieldSymbol, string> fieldToStandardNameMap;

            public PropertyToFieldMatcher(IEnumerable<IFieldSymbol> fields)
            {
                // Calculate and cache the standardised versions of the field names to avoid
                // calculating them every time
                fieldToStandardNameMap = fields.ToDictionary(f => f, f => GetCanonicalName(f.Name));
            }

            public IFieldSymbol GetSingleMatchingFieldOrNull(IPropertySymbol propertySymbol)
            {
                var matchingFields = fieldToStandardNameMap.Keys
                    .Where(fieldSymbol => FieldMatchesTheProperty(fieldSymbol, propertySymbol))
                    .ToList();

                return matchingFields.Count != 1
                    ? null
                    : matchingFields[0];
            }

            private static string GetCanonicalName(string name) =>
                name.Replace("_", string.Empty);

            private static bool AreCanonicalNamesEqual(string name1, string name2) =>
                name1.Equals(name2, StringComparison.OrdinalIgnoreCase);

            private bool FieldMatchesTheProperty(IFieldSymbol field, IPropertySymbol property) =>
                // We're not caching the property name as only expect to be called once per property
                !field.IsConst
                && ((property.IsStatic && field.IsStatic) || (!property.IsStatic && !field.IsStatic))
                && field.Type.Equals(property.Type)
                && AreCanonicalNamesEqual(fieldToStandardNameMap[field], GetCanonicalName(property.Name));
        }
    }
}
