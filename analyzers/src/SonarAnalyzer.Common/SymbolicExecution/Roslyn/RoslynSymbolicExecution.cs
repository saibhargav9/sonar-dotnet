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
using System.Linq;
using Microsoft.CodeAnalysis;
using SonarAnalyzer.CFG.Roslyn;
using SonarAnalyzer.SymbolicExecution.Roslyn.OperationProcessors;
using StyleCop.Analyzers.Lightup;

namespace SonarAnalyzer.SymbolicExecution.Roslyn
{
    internal class RoslynSymbolicExecution
    {
        private readonly ControlFlowGraph cfg;
        private readonly SymbolicCheck[] checks;
        private readonly Queue<ExplodedNode> queue = new();
        private readonly SymbolicValueCounter symbolicValueCounter = new();

        public RoslynSymbolicExecution(ControlFlowGraph cfg, SymbolicCheck[] checks)
        {
            this.cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            this.checks = checks ?? throw new ArgumentNullException(nameof(checks));
            if (!checks.Any())
            {
                throw new ArgumentException("At least one check is expected", nameof(checks));
            }
        }

        public void Execute()
        {
            // ToDo: Forbid running twice
            if (!ProgramPoint.HasSupportedSize(cfg))
            {
                return;
            }
            queue.Enqueue(new ExplodedNode(cfg.EntryBlock, ProgramState.Empty));
            while (queue.Any())
            {
                var current = queue.Dequeue();
                var successors = current.Operation == null ? ProcessBranching(current) : ProcessOperation(current);
                foreach (var node in successors)
                {
                    queue.Enqueue(node);
                }
            }

            NotifyExecutionCompleted();
        }

        private IEnumerable<ExplodedNode> ProcessBranching(ExplodedNode node)
        {
            // ToDo: This is a temporary simplification until we support proper branching. This only continues to the next ordinal block
            if (node.Block.Kind == BasicBlockKind.Exit)
            {
                InvokeChecks(new SymbolicContext(symbolicValueCounter, null, node.State), x => x.ExitReached);
            }
            else if (node.Block.ContainsThrow())
            {
                yield return new ExplodedNode(cfg.ExitBlock, node.State);
            }
            else
            {
                yield return new ExplodedNode(cfg.Blocks[node.Block.Ordinal + 1], node.State);
            }
        }

        private IEnumerable<ExplodedNode> ProcessOperation(ExplodedNode node)
        {
            var context = new SymbolicContext(symbolicValueCounter, node.Operation, node.State);
            context = InvokeChecks(context, x => x.PreProcess);
            if (context != null)
            {
                context = EnsureContext(context, ProcessOperation(context));
                context = InvokeChecks(context, x => x.PostProcess);
                if (context != null)
                {
                    yield return node.CreateNext(context.State);
                }
            }
        }

        private static ProgramState ProcessOperation(SymbolicContext context) =>
            context.Operation.Instance.Kind switch
            {
                OperationKindEx.SimpleAssignment => SimpleAssignment.Process(context, ISimpleAssignmentOperationWrapper.FromOperation(context.Operation.Instance)),
                OperationKindEx.LocalReference => References.Process(context, ILocalReferenceOperationWrapper.FromOperation(context.Operation.Instance)),
                OperationKindEx.ParameterReference => References.Process(context, IParameterReferenceOperationWrapper.FromOperation(context.Operation.Instance)),
                OperationKindEx.Conversion => Conversion.Process(context, IConversionOperationWrapper.FromOperation(context.Operation.Instance)),
                _ => context.State
            };

        private SymbolicContext InvokeChecks(SymbolicContext context, Func<SymbolicCheck, Func<SymbolicContext, ProgramState>> checkDelegate)
        {
            foreach (var check in checks)
            {
                var checkMethod = checkDelegate(check);
                var newState = checkMethod(context);
                if (newState == null)
                {
                    return null;
                }

                context = EnsureContext(context, newState);
            }
            return context;
        }

        private void NotifyExecutionCompleted()
        {
            foreach (var check in checks)
            {
                check.ExecutionCompleted();
            }
        }

        private SymbolicContext EnsureContext(SymbolicContext current, ProgramState newState) =>
            current.State == newState ? current : new SymbolicContext(symbolicValueCounter, current.Operation, newState);
    }
}
