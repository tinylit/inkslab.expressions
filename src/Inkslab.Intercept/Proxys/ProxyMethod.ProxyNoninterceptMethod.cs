using Inkslab.Emitters;
using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.Intercept.Proxys
{
    public abstract partial class ProxyMethod
    {
        private class ProxyNoninterceptMethod : ProxyMethod
        {
            public ProxyNoninterceptMethod(MethodInfo method, IList<CustomAttributeData> attributeDatas) : base(method, attributeDatas)
            {
            }

            public override bool IsRequired() => false;

            protected override MethodEmitter OverrideMethod(ClassEmitter classEmitter, FieldEmitter servicesAst, Expression instanceAst, MethodInfo methodInfo) => throw new System.NotImplementedException();
        }
    }
}
