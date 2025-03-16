using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;

using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp
{
	public partial class CSharpAstBuilder
	{
		EnumValueDisplayMode DetectBestEnumValueDisplayMode(ITypeDefinition typeDef)
		{
			if (typeDef.HasAttribute(KnownAttribute.Flags))
				return EnumValueDisplayMode.AllHex;
			bool first = true;
			long firstValue = 0, previousValue = 0;
			bool allPowersOfTwo = true;
			bool allConsecutive = true;
			foreach (var field in typeDef.Fields)
			{
				if (CSharpDecompiler.MemberIsHidden(field.MetadataToken, context.Settings))
					continue;
				object constantValue = field.GetConstantValue();
				if (constantValue == null)
					continue;
				long currentValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constantValue, false);
				allConsecutive = allConsecutive && (first || previousValue + 1 == currentValue);
				// N & (N - 1) == 0, iff N is a power of 2, for all N != 0.
				// We define that 0 is a power of 2 in the context of enum values.
				allPowersOfTwo = allPowersOfTwo && unchecked(currentValue & (currentValue - 1)) == 0;
				if (first)
				{
					firstValue = currentValue;
					first = false;
				}
				else if (currentValue <= previousValue)
				{
					// If the values are out of order, we fallback to displaying all values.
					return EnumValueDisplayMode.All;
				}
				else if (!allConsecutive && !allPowersOfTwo)
				{
					// We already know that the values are neither consecutive nor all powers of 2,
					// so we can abort, and just display all values as-is.
					return EnumValueDisplayMode.All;
				}
				previousValue = currentValue;
			}
			if (allPowersOfTwo)
			{
				if (previousValue > 8)
				{
					// If all values are powers of 2 and greater 8, display all enum values, but use hex.
					return EnumValueDisplayMode.AllHex;
				}
				else if (!allConsecutive)
				{
					// If all values are powers of 2, display all enum values.
					return EnumValueDisplayMode.All;
				}
			}
			if (context.Settings.AlwaysShowEnumMemberValues)
			{
				// The user always wants to see all enum values, but we know hex is not necessary.
				return EnumValueDisplayMode.All;
			}
			// We know that all values are consecutive, so if the first value is not 0
			// display the first enum value only.
			return firstValue == 0 ? EnumValueDisplayMode.None : EnumValueDisplayMode.FirstOnly;
		}

		private bool IsCovariantReturnOverride(IEntity entity)
		{
			if (!context.Settings.CovariantReturns)
				return false;
			if (!entity.HasAttribute(KnownAttribute.PreserveBaseOverrides))
				return false;
			return true;
		}

		IEnumerable<EntityDeclaration> AddInterfaceImplHelpers(EntityDeclaration memberDecl, ICSharpCode.Decompiler.TypeSystem.IMethod method, TypeSystemAstBuilder astBuilder)
		{
			if (!memberDecl.GetChildByRole(EntityDeclaration.PrivateImplementationTypeRole).IsNull)
			{
				yield break; // cannot create forwarder for existing explicit interface impl
			}
			if (method.IsStatic)
			{
				yield break; // cannot create forwarder for static interface impl
			}
			if (memberDecl.HasModifier(Modifiers.Extern))
			{
				yield break; // cannot create forwarder for extern method
			}
			var genericContext = new Decompiler.TypeSystem.GenericContext(method);
			var methodHandle = (MethodDef)method.MetadataToken;
			foreach (var h in methodHandle.Overrides) {
				ICSharpCode.Decompiler.TypeSystem.IMethod m = typeSystem.MainModule.ResolveMethod(h.MethodDeclaration, genericContext);
				if (m == null || m.DeclaringType.Kind != TypeKind.Interface)
					continue;
				var methodDecl = new MethodDeclaration();
				methodDecl.ReturnType = memberDecl.ReturnType.Clone();
				methodDecl.PrivateImplementationType = astBuilder.ConvertType(m.DeclaringType);
				methodDecl.Name = m.Name;
				methodDecl.TypeParameters.AddRange(memberDecl.GetChildrenByRole(Roles.TypeParameter)
												   .Select(n => (TypeParameterDeclaration)n.Clone()));
				methodDecl.Parameters.AddRange(memberDecl.GetChildrenByRole(Roles.Parameter).Select(n => n.Clone()));
				methodDecl.Constraints.AddRange(memberDecl.GetChildrenByRole(Roles.Constraint)
												.Select(n => (Constraint)n.Clone()));

				methodDecl.Body = new BlockStatement();
				methodDecl.Body.AddChild(new Comment(
					"ILSpy generated this explicit interface implementation from .override directive in " + memberDecl.Name),
					Roles.Comment);

				var member = new MemberReferenceExpression {
					Target = new ThisReferenceExpression().WithAnnotation(methodHandle.DeclaringType),
					MemberNameToken = Identifier.Create(memberDecl.Name).WithAnnotation(method.OriginalMember)
				}.WithAnnotation(method.OriginalMember);
				member.TypeArguments.AddRange(methodDecl.TypeParameters.Select(tp => new SimpleType(tp.Name)));

				var forwardingCall = new InvocationExpression(member,
					methodDecl.Parameters.Select(CSharpDecompiler.ForwardParameter)
				).WithAnnotation(method.OriginalMember);
				if (m.ReturnType.IsKnownType(KnownTypeCode.Void))
				{
					methodDecl.Body.Add(new ExpressionStatement(forwardingCall));
				}
				else
				{
					methodDecl.Body.Add(new ReturnStatement(forwardingCall));
				}
				yield return methodDecl;
			}
		}

		void AddDefinesForConditionalAttributes(ILFunction function)
		{
			foreach (var call in function.Descendants.OfType<CallInstruction>()) {
				var attr = call.Method.GetAttribute(KnownAttribute.Conditional, inherit: true);
				var symbolName = attr?.FixedArguments.FirstOrDefault().Value as string;
				if (symbolName == null || !currentDecompileRun.DefinedSymbols.Add(symbolName))
					continue;
				syntaxTree.InsertChildAfter(null, new PreProcessorDirective(PreProcessorDirectiveType.Define, symbolName), Roles.PreProcessorDirective);
			}
		}
	}
}
