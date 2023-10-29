using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 初始成员。
    /// </summary>
    public class MemberInitExpression : Expression
    {
        internal MemberInitExpression(NewExpression newExpression, IReadOnlyList<MemberAssignment> bindings) : base(newExpression.RuntimeType)
        {
            NewExpression = newExpression ?? throw new ArgumentNullException(nameof(newExpression));
            Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        /// <summary>
        /// 实例化表达式。
        /// </summary>
        public NewExpression NewExpression { get; }

        /// <summary>
        /// 初始化成员。
        /// </summary>
        public IReadOnlyList<MemberAssignment> Bindings { get; }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            NewExpression.Load(ilg);

            if (Bindings.Count > 0)
            {
                if (RuntimeType.IsValueType)
                {
                    var local = ilg.DeclareLocal(RuntimeType);

                    ilg.Emit(OpCodes.Stloc, local);
                    ilg.Emit(OpCodes.Ldloca, local);

                    EmitMemberInit(ilg, Bindings, false, RuntimeType);

                    ilg.Emit(OpCodes.Ldloc, local);
                }
                else
                {
                    EmitMemberInit(ilg, Bindings, true, RuntimeType);
                }
            }
        }

        private void EmitMemberInit(ILGenerator ilg, IReadOnlyList<MemberAssignment> bindings, bool keepOnStack, Type objectType)
        {
            for (int i = 0, len = bindings.Count; i < len; i++)
            {
                if (keepOnStack || i < len - 1)
                {
                    ilg.Emit(OpCodes.Dup);
                }

                EmitBinding(ilg, bindings[i], objectType);
            }
        }

        private void EmitBinding(ILGenerator ilg, MemberAssignment binding, Type objectType)
        {
            binding.Expression.Load(ilg);

            switch (binding.Member)
            {
                case PropertyInfo propertyInfo:
                    EmitCall(ilg, objectType, propertyInfo.SetMethod ?? propertyInfo.GetSetMethod(true));
                    break;
                case FieldInfo fieldInfo:
                    ilg.Emit(OpCodes.Stfld, fieldInfo);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool UseVirtual(MethodInfo mi)
        {
            // There are two factors: is the method static, virtual or non-virtual instance?
            // And is the object ref or value?
            // The cases are:
            //
            // static, ref:     call
            // static, value:   call
            // virtual, ref:    callvirt
            // virtual, value:  call -- eg, double.ToString must be a non-virtual call to be verifiable.
            // instance, ref:   callvirt -- this looks wrong, but is verifiable and gives us a free null check.
            // instance, value: call
            //
            // We never need to generate a nonvirtual call to a virtual method on a reference type because
            // expression trees do not support "base.Foo()" style calling.
            // 
            // We could do an optimization here for the case where we know that the object is a non-null
            // reference type and the method is a non-virtual instance method.  For example, if we had
            // (new Foo()).Bar() for instance method Bar we don't need the null check so we could do a
            // call rather than a callvirt.  However that seems like it would not be a very big win for
            // most dynamically generated code scenarios, so let's not do that for now.

            if (mi.IsStatic)
            {
                return false;
            }

            if (mi.DeclaringType.IsValueType)
            {
                return false;
            }

            return true;
        }

        private void EmitCall(ILGenerator ilg, Type objectType, MethodInfo method)
        {
            if (method.CallingConvention == CallingConventions.VarArgs)
            {
                throw new NotSupportedException();
            }

            OpCode callOp = UseVirtual(method) ? OpCodes.Callvirt : OpCodes.Call;

            if (callOp == OpCodes.Callvirt && objectType.IsValueType)
            {
                ilg.Emit(OpCodes.Constrained, objectType);
            }

            ilg.Emit(callOp, method);
        }
    }
}
