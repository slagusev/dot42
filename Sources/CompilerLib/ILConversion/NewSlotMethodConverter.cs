﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Dot42.CecilExtensions;
using Dot42.CompilerLib.Ast.Extensions;
using Dot42.CompilerLib.Naming;
using Dot42.CompilerLib.Reachable;
using Dot42.LoaderLib.Extensions;
using Mono.Cecil;

namespace Dot42.CompilerLib.ILConversion
{
    [Export(typeof (ILConverterFactory))]
    internal class NewSlotMethodConverter : ILConverterFactory
    {
        /// <summary>
        /// Low values come first
        /// </summary>
        public int Priority
        {
            get { return 25; }
        }

        /// <summary>
        /// Create the converter
        /// </summary>
        public ILConverter Create()
        {
            return new Converter();
        }

        private class Converter : ILConverter
        {
            private List<MethodDefinition> reachableMethods;
            private NameSet methodNames;

            /// <summary>
            /// Rename "new" methods that override a final base method.
            /// </summary>
            public void Convert(ReachableContext reachableContext)
            {
                // Collect all names
                var newSlotMethods = reachableContext.ReachableTypes.SelectMany(x => x.Methods).
                    Where(m => m.IsHideBySig && !m.IsStatic && !m.IsRuntimeSpecialName && !m.DeclaringType.IsInterface && (m.GetDexOrJavaImportAttribute() == null)).ToList();
                if (newSlotMethods.Count == 0)
                    return;

                // Go over each method, find methods to rename
                var methodsToRename = new HashSet<MethodDefinition>();
                foreach (var method in newSlotMethods)
                {
                    // Is the direct base method final?
                    var baseMethod = method.GetBaseMethod();
                    if (baseMethod == null)
                        continue;
                    if ((baseMethod.IsVirtual || baseMethod.IsAbstract) && !baseMethod.IsFinal)
                        continue;

                    methodsToRename.Add(method);
                    methodsToRename.Add(baseMethod);
                }
                if (methodsToRename.Count == 0)
                    return;

                // Initialize some sets
                reachableMethods = reachableContext.ReachableTypes.SelectMany(x => x.Methods).Where(m => m.IsReachable).OrderBy(x => x.FullName).ToList();
                methodNames = new NameSet(reachableMethods.Select(m => m.Name));

                // Rename methods that need renaming
                foreach (var method in methodsToRename)
                {
                    // Create new name
                    var newName = methodNames.GetUniqueName(method.Name);

                    // Find all methods that derive from method
                    var groupMethods = new HashSet<MethodDefinition> { method };
                    foreach (var otherMethod in reachableMethods.Except(methodsToRename))
                    {
                        var renamedBaseMethod = otherMethod.GetBaseMethods().FirstOrDefault(methodsToRename.Contains);
                        if (renamedBaseMethod == method)
                        {
                            // Rename this other method as well
                            groupMethods.Add(otherMethod);
                        }
                    }

                    // Add explicit implementations for interface methods 
                    foreach (var iMethod in method.GetBaseInterfaceMethods())
                    {
                        var iMethodIsJavaWithGenericParams = iMethod.IsJavaMethodWithGenericParams();
                        var explicitName = methodNames.GetUniqueName(method.DeclaringType.Name + "_" + iMethod.Name);
                        var stub = InterfaceHelper.CreateExplicitStub(method, explicitName, iMethod, iMethodIsJavaWithGenericParams);
                        stub.IsPrivate = true;
                        stub.IsFinal = false;
                        stub.IsVirtual = true;
                    }

                    // Rename all methods in the group
                    foreach (var m in groupMethods)
                    {
                        Rename(m, newName);
                    }
                }
            }

            /// <summary>
            /// Rename the given method and all references to it from code.
            /// </summary>
            private void Rename(MethodDefinition method, string newName)
            {
                // Rename reference to method
                foreach (var body in reachableMethods.Select(x => x.Body).Where(x => x != null))
                {
                    var resolver = new GenericsResolver(method.DeclaringType);
                    foreach (var ins in body.Instructions.Where(x => x.Operand is MethodReference))
                    {
                        var methodRef = ((MethodReference) ins.Operand).GetElementMethod();
                        if (!ReferenceEquals(methodRef, method) && methodRef.AreSameIncludingDeclaringType(method, resolver.Resolve))
                        {
                            methodRef.Name = newName;
                        }
                    }
                }
                // Rename method itself
                method.Name = newName;
            }
        }
    }
}