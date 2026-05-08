![Inkslab](inkslab-mini.jpg 'Logo')

# Inkslab.Expressions

[![NuGet](https://img.shields.io/nuget/v/Inkslab.Expressions.svg)](https://www.nuget.org/packages/Inkslab.Expressions/)
![Downloads](https://img.shields.io/nuget/dt/Inkslab.Expressions)
![License](https://img.shields.io/github/license/tinylit/inkslab.expressions.svg)

> 基于 `System.Reflection.Emit` 的动态类型生成器：用类似 `System.Linq.Expressions` 的工厂式 API 直接编写 IL，无需手写 OpCode。
>
> 本文件同时作为 NuGet 包的 README，并被设计为 **AI 编程助手可直接消费的规格文档**：所有 API 均给出**签名 + 约束 + 示例**三段式描述，章节锚点稳定不变。

---

## 安装

```powershell
PM> Install-Package Inkslab.Expressions
```

| 目标框架 | 状态 |
|----------|------|
| `net461` / `net462` / `net47x` / `net48` | ✅ 支持，可保存物理程序集 |
| `netstandard2.1` | ✅ 支持 |
| `net6.0` | ✅ 支持 |

---

## AI Reading Notes（机器优先速查）

> 本节为给 AI 编程助手的硬性规则。生成代码前必须读完。

### 全局规则

1. **一切节点必须通过 `Expression` 静态工厂创建**，禁止 `new XxxExpression(...)`。
2. **方法体即 `BlockExpression`**：`MethodEmitter` / `ConstructorEmitter` 继承自 `BlockExpression`，调用 `.Append(expr)` 顺序追加语句。
3. **块复用是只读的**：`BlockExpression` 一旦被 `Append` 进父块即变只读，再追加抛 `AstException`。复用前先构建完整再嵌入。
4. **变量不可跨方法体**：同一 `VariableExpression` 实例只能用于一个 `MethodEmitter`，跨方法体会导致 `LocalBuilder` 冲突。
5. **`CanWrite == true` 才能做赋值左值**：仅 `VariableExpression`、可写 `ParameterExpression`、非 `readonly` `FieldExpression`、有 setter 的 `PropertyExpression`、`ArrayIndexExpression` 满足。
6. **空 `void` 块自动 Pop**：当 `BlockExpression.IsVoid == true` 且追加非 void 表达式时，框架自动插入 `Nop` 弹栈，无需手动平衡。
7. **生命周期**：`new ModuleEmitter(...)` → `module.DefineType(...)` → 定义成员 → `cls.CreateType()` → 反射使用运行时 `Type`。
8. **泛型方法必须先 `MakeGenericMethod`**：含 `IsGenericParameter` 的 `MethodInfo` 直接传入 `Expression.Call` 会抛异常。
9. **`Equal` / `NotEqual` 自带回退链**：算术原生 `ceq` → 用户 `operator ==` / `!=` → 可空提升比较 → `IEquatable<T>.Equals(T)` → 重写的 `Equals(object)` → `object.ReferenceEquals`（仅 `object` vs `object`）。回退到实例 `Equals` 时按 `callvirt` 发射，遵循虚分派；都不满足才抛 `AstException`。
10. **可空类型自动提升**（lifted）：`Equal` / `NotEqual` 与一元 `Negate` / `Not` / `Increment` / `Decrement` / `IncrementAssign` / `DecrementAssign` 在 `Nullable<T>` 上自动按 C# 提升语义发射 IL（任一 `null` 则结果按规范决定，否则在底层类型上运算后装回 `Nullable<T>`）。开发者无需手写 `HasValue` / `GetValueOrDefault` 包裹。

### 命名空间

```csharp
using Inkslab;          // ModuleEmitter、Expression、ClassEmitter ... 全部在此
```

### 错误模式速查

| 错误现象 | 根因 |
|----------|------|
| `AstException: 块已只读` | `BlockExpression` 已经被 Append 进其它块 |
| `LocalBuilder` 跨方法异常 | `VariableExpression` 在两个 `MethodEmitter` 中重复使用 |
| `ArgumentException` 类型不匹配 | 赋值/三目/Coalesce 的左右两侧类型不满足兼容规则 |
| `Break`/`Continue` 越界 | `BreakExpression` / `ContinueExpression` 没有出现在某个循环块（`LoopExpression` / `ForEachExpression`）内部 |
| `Switch` 分支编译失败 | `Case(ConstantExpression)` 与 `Case(VariableExpression)` 在同一个 Switch 中混用 |
| `AstException: 不支持X与Y的Equal运算！` | 引用类型既无 `operator ==`，也未实现 `IEquatable<T>` 或重写 `Equals(object)`，且不是 `object`-`object` 比较 |
| `AstException: 表达式"void"无效！` | 把 `IfThen` / `IfThenElse` / 任何 `IsVoid` 表达式当作 `Convert` / `TypeIs` / `TypeAs` / `Return` 的子表达式 |
| `AstException: 参数不是异常类型!` | `Expression.Throw(expr)` 的参数运行时类型不继承自 `System.Exception` |

---

## 5 行入门

```csharp
var module = new ModuleEmitter("Demo");
var cls = module.DefineType("Demo.Hello", TypeAttributes.Public | TypeAttributes.Class);
var add = cls.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
var a = add.DefineParameter(typeof(int), ParameterAttributes.None, "a");
var b = add.DefineParameter(typeof(int), ParameterAttributes.None, "b");
add.Append(Expression.Add(a, b));            // 方法体：return a + b;
Type t = cls.CreateType();
```

---

## 架构总览

```
ModuleEmitter                      // 动态程序集（每个模块 = 一个 AssemblyBuilder）
  └── ClassEmitter                 // 一个动态类型，继承 AbstractTypeEmitter
        ├── FieldEmitter           // 字段（本身是 Expression，可读可写）
        ├── PropertyEmitter        // 属性（getter/setter 通过 MethodEmitter）
        ├── ConstructorEmitter     // 构造函数体（继承 BlockExpression）
        └── MethodEmitter          // 方法体（继承 BlockExpression）
              └── Expression.*     // 节点工厂：赋值/分支/循环/调用/...
```

**生命周期约束**：

| 阶段 | 允许的操作 |
|------|-----------|
| `DefineType` 后、`CreateType` 前 | 定义字段、属性、方法、构造函数、嵌套类型；向方法体 Append 表达式 |
| `CreateType` 之后 | 反射使用，**禁止**再修改任何 emitter |

---

## ModuleEmitter — 动态程序集

### 构造签名

```csharp
ModuleEmitter()
ModuleEmitter(string assemblyName)
ModuleEmitter(string assemblyName, string moduleFileName)
ModuleEmitter(bool savePhysicalAssembly)                              // NET Framework 才有效
ModuleEmitter(bool savePhysicalAssembly, string assemblyName)
ModuleEmitter(bool savePhysicalAssembly, string assemblyName, string moduleFileName)
ModuleEmitter(INamingScope naming, string assemblyName, string moduleFileName)          // 嵌套模块
ModuleEmitter(bool savePhysicalAssembly, INamingScope namingScope, string assemblyName, string moduleFileName)
```

**默认值常量**：

| 常量 | 值 |
|------|---|
| `ModuleEmitter.DEFAULT_ASSEMBLY_NAME` | `"Inkslab.Override"` |
| `ModuleEmitter.DEFAULT_FILE_NAME` | `"Inkslab.Override.dll"` |

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `AssemblyFileName` | `string` | 程序集文件名 |
| `AssemblyDirectory` | `string` | 程序集输出目录 |

### 主要方法

| 方法 | 返回 | 说明 |
|------|------|------|
| `DefineType(name)` | `ClassEmitter` | 顶级类（默认属性） |
| `DefineType(name, attributes)` | `ClassEmitter` | 指定属性 |
| `DefineType(name, attributes, baseType)` | `ClassEmitter` | 指定基类 |
| `DefineType(name, attributes, baseType, interfaces[])` | `ClassEmitter` | 基类 + 多接口 |
| `DefineEnum(name, attributes, underlyingType)` | `EnumEmitter` | 枚举类型 |
| `BeginScope()` | `INamingScope` | 开始命名作用域 |
| `SaveAssembly()` | `string` | 仅 NET Framework，保存到磁盘，返回路径 |

---

## ClassEmitter / AbstractTypeEmitter — 动态类型

> `ClassEmitter` 与 `EnumEmitter`、`NestedClassEmitter` 共同继承自 `AbstractTypeEmitter`。下表方法都属于基类。

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 类型名称 |
| `BaseType` | `Type` | 基类类型 |
| `IsGenericType` | `bool` | 是否为泛型类型 |

### 方法

| 方法 | 返回 | 说明 |
|------|------|------|
| `DefineField(name, type, attributes)` | `FieldEmitter` | 字段（最常用重载） |
| `DefineField(name, type)` | `FieldEmitter` | 字段（默认 `FieldAttributes.Private`） |
| `DefineField(name, type, serializable)` | `FieldEmitter` | 字段（支持序列化标记） |
| `DefineField(fieldInfo)` | `FieldEmitter` | 从已有 `FieldInfo` 克隆字段定义 |
| `DefineProperty(name, attributes, type)` | `PropertyEmitter` | 属性，需后续 `SetGetMethod` / `SetSetMethod` |
| `DefineProperty(name, attributes, type, arguments[])` | `PropertyEmitter` | 带索引参数的属性（如 `this[int]`） |
| `DefineMethod(name, attrs, returnType)` | `MethodEmitter` | 普通方法 |
| `DefineMethodOverride(ref methodInfo)` | `MethodEmitter` | 显式接口方法实现 |
| `DefineConstructor(attributes)` | `ConstructorEmitter` | 实例构造函数 |
| `DefineConstructor(attributes, conventions)` | `ConstructorEmitter` | 指定调用约定的构造函数 |
| `DefineDefaultConstructor()` | `void` | 自动生成默认无参构造 |
| `DefineTypeInitializer()` | `TypeInitializerEmitter` | 静态构造函数（`static MyType() { }`） |
| `DefineNestedType(name)` | `NestedClassEmitter` | 嵌套类型（默认属性） |
| `DefineNestedType(name, attributes)` | `NestedClassEmitter` | 嵌套类型 |
| `DefineNestedType(name, attributes, baseType)` | `NestedClassEmitter` | 指定基类 |
| `DefineNestedType(name, attributes, baseType, interfaces[])` | `NestedClassEmitter` | 基类 + 接口 |
| `DefineGenericParameters(typeArguments)` | `GenericTypeParameterBuilder[]` | 定义泛型参数 |
| `OverrideMethod(MethodInfo)` | `MethodEmitter` | 重写基类虚方法或实现接口方法 |
| `BeginScope()` | `INamingScope` | 开始命名作用域 |
| `GetInterfaces()` | `Type[]` | 获取实现的接口列表 |
| `GetGenericArguments()` | `Type[]` | 获取泛型参数 |
| `IsCreated()` | `bool` | 类型是否已创建 |
| `CreateType()` | `Type` | **结束定义**，返回运行时类型 |

> **关于 `NestedClassEmitter.UncompiledType`**：仅 `NestedClassEmitter` 对外暴露 `public TypeBuilder UncompiledType`，用于需要直接访问未编译类型令牌的场景。`Convert` / `TypeIs` / `TypeAs` 已提供 `AbstractTypeEmitter` 重载，推荐优先使用而非手动访问此属性。

### 自定义特性（Custom Attributes）

所有 `AbstractTypeEmitter` 子类（`ClassEmitter`、`EnumEmitter`、`NestedClassEmitter`）均支持：

| 方法 | 说明 |
|------|------|
| `SetCustomAttribute(CustomAttributeData)` | 通过 `CustomAttributeData` 设置特性 |
| `SetCustomAttribute(ConstructorInfo, byte[])` | 通过构造函数 + 二进制数据设置特性 |
| `DefineCustomAttribute(CustomAttributeBuilder)` | 通过 `CustomAttributeBuilder` 定义特性 |
| `DefineCustomAttribute<TAttribute>()` | 泛型快捷方式：`TAttribute : Attribute, new()` |
| `DefineCustomAttribute(CustomAttributeData)` | 通过 `CustomAttributeData` 定义特性 |

### 示例

```csharp
var cls = module.DefineType(
    "Demo.Foo",
    TypeAttributes.Public | TypeAttributes.Class,
    typeof(MyBase),
    new[] { typeof(IDisposable) });

var field = cls.DefineField("_x", typeof(int), FieldAttributes.Private);
var ctor  = cls.DefineConstructor(MethodAttributes.Public);
ctor.Append(Expression.Assign(field, Expression.Constant(0)));

Type runtime = cls.CreateType();
```

---

## MethodEmitter — 方法体

继承自 `BlockExpression`，本身就是一个方法体级的代码块。

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 方法名称 |
| `ReturnType` | `Type` | 返回值类型（同 `RuntimeType`） |
| `Attributes` | `MethodAttributes` | 方法特性（`Public` / `Static` / `Virtual` 等） |
| `IsStatic` | `bool` | 是否为静态方法 |
| `IsGenericMethod` | `bool` | 是否为泛型方法 |
| `DeclaringType` | `AbstractTypeEmitter` | 声明此方法的类型 |

### 方法

| 方法 | 说明 |
|------|------|
| `DefineParameter(type, attributes, name)` | 定义参数，返回 `ParameterEmitter`（继承 `ParameterExpression`） |
| `DefineParameter(type, name)` | 定义参数（简化重载，默认属性） |
| `DefineParameter(ParameterInfo)` | 从已有 `ParameterInfo` 克隆参数定义 |
| `Append(Expression)` | 追加一条语句；返回自身以支持链式 |
| `MakeGenericMethod(Type[])` | 获取该方法的泛型实例化版本 |
| `GetParameters()` | 获取所有已定义的 `ParameterEmitter` |
| `SetCustomAttribute(CustomAttributeBuilder)` | 添加特性 |
| `SetCustomAttribute<TAttribute>()` | 泛型快捷方式添加特性 |
| `SetCustomAttribute(CustomAttributeData)` | 通过 `CustomAttributeData` 添加特性 |

### 返回值规则

- **方法签名为 `void`**：可不写 `Return`，方法体执行完即返回。
- **方法签名有返回值**：方法体需以一个返回该类型的表达式收尾，或显式 `Expression.Return(value)`。
- 框架在 Emit 阶段自动绑定方法出口标签，所有 `Return` 都是 `br` 到该出口。

```csharp
var add = cls.DefineMethod("Add",
    MethodAttributes.Public | MethodAttributes.Static, typeof(int));
var a = add.DefineParameter(typeof(int), ParameterAttributes.None, "a");
var b = add.DefineParameter(typeof(int), ParameterAttributes.None, "b");
add.Append(Expression.Add(a, b));   // 末尾表达式即返回值
```

---

## ConstructorEmitter — 构造函数

继承自 `BlockExpression`。框架默认在体首自动调用基类无参构造，如需调用自定义基类构造，使用 `InvokeBaseConstructor`。

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 构造函数名称（即类型名） |
| `Attributes` | `MethodAttributes` | 构造特性 |
| `Conventions` | `CallingConventions` | 调用约定 |

### 方法

| 方法 | 说明 |
|------|------|
| `DefineParameter(type, attributes, name)` | 定义参数 |
| `DefineParameter(ParameterInfo)` | 从已有 `ParameterInfo` 克隆参数 |
| `Append(Expression)` | 追加语句 |
| `GetParameters()` | 获取所有已定义的参数 |
| `InvokeBaseConstructor()` | 调用基类无参构造 |
| `InvokeBaseConstructor(ConstructorInfo)` | 调用指定基类构造（无参） |
| `InvokeBaseConstructor(ConstructorInfo, params Expression[])` | 调用指定基类构造并传参 |

```csharp
var ctor = cls.DefineConstructor(MethodAttributes.Public);
var p = ctor.DefineParameter(typeof(string), ParameterAttributes.None, "value");
ctor.InvokeBaseConstructor(baseCtorInfo, p);            // : base(value)
ctor.Append(Expression.Assign(field, p));               // _field = value;
```

---

## FieldEmitter / PropertyEmitter

`FieldEmitter` 与 `PropertyEmitter` 本身实现了 `Expression`：

- 直接 `Append(field)` 表示读取字段值。
- `Expression.Assign(field, value)` 表示写入字段。
- 属性访问通过 `Expression.Property(...)` 或直接使用 `PropertyEmitter`。

### FieldEmitter

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 字段名称 |
| `Attributes` | `FieldAttributes` | 字段特性 |
| `CanWrite` | `bool` | 是否可写（非 `Literal` 即为 `true`） |
| `IsStatic` | `bool` | 是否为静态字段 |
| `DefaultValue` | `object` | 字段默认值 |

| 方法 | 说明 |
|------|------|
| `SetCustomAttribute<TAttribute>()` | 泛型添加特性 |
| `SetCustomAttribute(CustomAttributeData)` | 通过 `CustomAttributeData` 添加特性 |
| `SetCustomAttribute(CustomAttributeBuilder)` | 通过 `CustomAttributeBuilder` 添加特性 |

### PropertyEmitter

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 属性名称 |
| `Attributes` | `PropertyAttributes` | 属性特性 |
| `CanRead` | `bool` | 是否可读（已设置 getter） |
| `CanWrite` | `bool` | 是否可写（已设置 setter） |
| `IsStatic` | `bool` | 是否为静态属性 |
| `DefaultValue` | `object` | 属性默认值 |
| `ParameterTypes` | `Type[]` | 索引参数类型（如 `this[int]` 则为 `[typeof(int)]`） |

| 方法 | 返回 | 说明 |
|------|------|------|
| `SetGetMethod(MethodEmitter)` | `PropertyEmitter` | 设置 getter |
| `SetSetMethod(MethodEmitter)` | `PropertyEmitter` | 设置 setter |
| `SetCustomAttribute<TAttribute>()` | `void` | 泛型添加特性 |
| `SetCustomAttribute(CustomAttributeData)` | `void` | 添加特性 |
| `SetCustomAttribute(CustomAttributeBuilder)` | `void` | 添加特性 |

```csharp
var backing = cls.DefineField("_name", typeof(string), FieldAttributes.Private);

var getter = cls.DefineMethod("get_Name",
    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
    typeof(string));
getter.Append(backing);                                    // return _name;

var setter = cls.DefineMethod("set_Name",
    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
    typeof(void));
var v = setter.DefineParameter(typeof(string), ParameterAttributes.None, "value");
setter.Append(Expression.Assign(backing, v));              // _name = value;

var prop = cls.DefineProperty("Name", PropertyAttributes.None, typeof(string));
prop.SetGetMethod(getter);
prop.SetSetMethod(setter);
```

---

## EnumEmitter — 枚举

```csharp
var e = module.DefineEnum("Demo.Color", TypeAttributes.Public, typeof(int));
e.DefineLiteral("Red",   0);
e.DefineLiteral("Green", 1);
e.DefineLiteral("Blue",  2);
Type colorType = e.CreateType();
```

### 方法

| 方法 | 返回 | 说明 |
|------|------|------|
| `DefineLiteral(name, value)` | `FieldEmitter` | 定义枚举字面量 |
| `DefineCustomAttribute<TAttribute>()` | `void` | 泛型添加特性 |
| `DefineCustomAttribute(CustomAttributeBuilder)` | `void` | 添加特性 |
| `DefineCustomAttribute(CustomAttributeData)` | `void` | 添加特性 |
| `SetCustomAttribute(ConstructorInfo, byte[])` | `void` | 二进制方式设置特性 |
| `IsCreated()` | `bool` | 枚举是否已创建 |
| `CreateType()` | `Type` | 创建枚举类型 |

---

# Expression — 表达式系统

所有节点继承自抽象基类 `Expression`（命名空间 `Inkslab`）。

## 基类核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `RuntimeType` | `Type` | 运行时返回类型 |
| `IsVoid` | `bool` | `RuntimeType == typeof(void)` |
| `IsContext` | `bool` | 是否为 `this` 上下文 |
| `CanWrite` | `bool` | 能否作为赋值左值（默认 `false`） |

**`CanWrite == true` 的节点**：`VariableExpression`、非 `in` 的 `ParameterExpression`、非 `readonly` `FieldExpression`、有 setter 的 `PropertyExpression`、`ArrayIndexExpression`。

### 静态成员

| 成员 | 类型 | 说明 |
|------|------|------|
| `EmptyAsts` | `Expression[]` | 空表达式数组，用于无参方法调用等场景 |

---

## 节点继承图

```
Expression                          // 抽象基类
  ├── ConstantExpression            // 常量
  ├── DefaultExpression             // default(T)
  ├── ThisExpression                // this
  ├── VariableExpression            // 局部变量（CanWrite=true）
  │     └── ParameterExpression     // 方法参数
  ├── MemberExpression              // 成员基类
  │     ├── FieldExpression
  │     └── PropertyExpression
  ├── BinaryExpression              // 二元运算
  ├── UnaryExpression               // 一元运算
  ├── ConvertExpression             // (T)expr
  ├── TypeIsExpression              // expr is T
  ├── TypeAsExpression              // expr as T
  ├── CoalesceExpression            // expr ?? fallback
  ├── ConditionExpression           // 三目
  ├── MethodCallExpression          // obj.Method(args)
  ├── InvocationExpression          // MethodBase.Invoke(obj, object[])
  ├── NewExpression                 // new T(args)
  ├── NewArrayExpression            // new T[n]
  ├── ArrayExpression               // new T[]{ ... }
  ├── ArrayIndexExpression          // arr[i]（CanWrite=true）
  ├── ArrayLengthExpression         // arr.Length
  ├── MemberInitExpression          // new T{ P = v, ... }
  ├── MemberAssignment              // 成员绑定
  ├── ReturnExpression              // return
  ├── ThrowExpression               // throw
  ├── GotoExpression                // goto
  ├── LabelExpression               // label:
  ├── BreakExpression               // break
  ├── ContinueExpression            // continue
  └── BlockExpression               // 语句块
        ├── LoopExpression          // while(true){...}
      ├── ForEachExpression       // for / foreach 统一循环
        ├── IfThenExpression
        ├── IfThenElseExpression
        ├── SwitchExpression
        ├── TryExpression
        └── MethodEmitter
```

---

## 工厂方法分组

下文每条 API 都按 **签名 → 约束 → 示例（必要时）** 三段格式给出。

### 1. 常量与默认值

```csharp
ConstantExpression Constant(object value);
ConstantExpression Constant(object value, Type constantType);
DefaultExpression  Default(Type defaultType);
```

**约束**：

- `Constant(object value)` 自动推断类型：`null → typeof(object)`，`MethodInfo → typeof(MethodInfo)`，`Type → typeof(Type)`。
- `ConstantExpression.IsNull == true` 表示该常量为 `null`。
- 支持值类型、引用类型、`Type` 对象、`MethodInfo` 对象。

---

### 2. 变量与参数

```csharp
VariableExpression Variable(Type variableType);
ParameterExpression Paramter(ParameterInfo parameter);   // 注意：方法名拼写为 Paramter
```

**约束**：

- `VariableExpression` 首次出现在 `Load(ILGenerator)` 时才分配 `LocalBuilder`，因此**同一实例不能跨方法体**使用。
- `Paramter` 仅用于包装已有的 `ParameterInfo`；通过 `MethodEmitter.DefineParameter` 得到的 `ParameterEmitter` 已经是 `ParameterExpression`。

---

### 3. this / base

```csharp
Expression This(AbstractTypeEmitter typeEmitter);
// base：先取 This，再访问 .Base
var thisExpr = (ThisExpression)Expression.This(cls);
Expression baseExpr = thisExpr.Base;
```

**约束**：

- `This` 不支持抽象类型。
- `Base` 不支持值类型（值类型无基类引用语义）。

---

### 4. 字段与属性

```csharp
FieldExpression    Field(FieldInfo field);                       // static
FieldExpression    Field(Expression instance, FieldInfo field);  // instance
PropertyExpression Property(PropertyInfo property);                       // static
PropertyExpression Property(Expression instance, PropertyInfo property);  // instance
```

**约束**：

- 静态成员重载要求 `field.IsStatic == true` / `property` 的访问器为静态。
- `FieldEmitter` 与 `PropertyEmitter` 本身就是表达式，可直接使用，无需再包一层 `Field(...)` / `Property(...)`。

---

### 5. 赋值

```csharp
Expression Assign(Expression left, Expression right);
```

**约束（任一满足即合法）**：

1. `left.RuntimeType.IsAssignableFrom(right.RuntimeType)`；
2. `right` 是 `ThisExpression`；
3. 两侧枚举的底层类型相同；
4. `right.RuntimeType == Nullable.GetUnderlyingType(left.RuntimeType)`。

`left.CanWrite` 必须为 `true`。

---

### 6. 类型转换与检查

```csharp
ConvertExpression  Convert(Expression body, Type convertToType);                   // (T)expr
ConvertExpression  Convert(Expression body, AbstractTypeEmitter typeEmitter);       // 传入类型发射器
TypeIsExpression   TypeIs(Expression body, Type bodyIsType);                       // expr is T
TypeIsExpression   TypeIs(Expression body, AbstractTypeEmitter typeEmitter);       // 传入类型发射器
TypeAsExpression   TypeAs(Expression body, Type bodyAsType);                       // expr as T
TypeAsExpression   TypeAs(Expression body, AbstractTypeEmitter typeEmitter);       // 传入类型发射器
CoalesceExpression Coalesce(Expression left, Expression right);                    // left ?? right
```

> `AbstractTypeEmitter` 重载可直接传入 `ClassEmitter` / `NestedClassEmitter` 实例，内部自动通过 `Value` 获取 `TypeBuilder`。

**约束**：

- `Convert`：`body.RuntimeType == convertToType` 时为零成本（不发射 IL）。
- `Coalesce`：`left.RuntimeType` 必须为引用类型或可空值类型。

---

### 7. 三目（必须有返回值）

```csharp
ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse);
ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type returnType);
```

**约束**：

- `test.RuntimeType == typeof(bool)`。
- **必须有返回值**：`ifTrue` / `ifFalse` 任一为 `void` 或显式指定 `returnType = typeof(void)` 均抛 `AstException("三目运算表达式必须有返回值，无返回值请使用 IfThenElse 表达式！")`。
- 不指定 `returnType` 时，`ifTrue` 与 `ifFalse` 必须类型相同；
- 指定 `returnType` 时，两分支必须可隐式转换为该类型，否则抛 `ArgumentException`。

---

### 8. IfThen / IfThenElse — 无返回值分支

```csharp
IfThenExpression     IfThen(Expression test, Expression ifTrue);
IfThenElseExpression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse);
```

**约束**：

- `test.RuntimeType == typeof(bool)`。
- 两者均为 **void 表达式**（`IsVoid == true`），不可作为有返回值方法的最终表达式直接返回。若所有分支已含 `Return`/`Throw`，末尾需追加 `Expression.Throw(...)` 表示不可达路径。
- 需要带返回值的条件表达式请使用 `Expression.Condition(...)`（三目）。

---

### 9. 方法调用 — 三种形态语义不同

| 工厂 | IL | 参数 | 适用 |
|------|----|------|------|
| `Call` | `callvirt`（virtual）/ `call`（其它） | 逐个 `Expression` | 编译期已知签名的常规调用 |
| `DeclaringCall` | 接口/抽象 → `callvirt`，其它 → `call` | 逐个 `Expression` | 显式调用声明类型实现，绕过子类重写 |
| `Invoke` | `MethodBase.Invoke(object, object[])` | 单个 `object[]` 表达式 | 运行期动态参数或动态方法 |

```csharp
// Call
Expression.Call(methodInfo);                           // 静态无参
Expression.Call(methodInfo, arg0, arg1);               // 静态有参
Expression.Call(instance, methodInfo);                 // 实例无参
Expression.Call(instance, methodInfo, arg0, arg1);     // 实例有参
Expression.Call(methodEmitter);                        // 同模块 MethodEmitter（this 自动注入）
Expression.Call(methodEmitter, arg0, arg1);            // 同模块 MethodEmitter 有参（this 自动注入）
Expression.Call(instance, methodEmitter, arg0, arg1);  // MethodEmitter 显式指定实例

// DeclaringCall
Expression.DeclaringCall(instance, methodInfo);
Expression.DeclaringCall(instance, methodInfo, arg0, arg1);

// Invoke（参数为 object[] 表达式）
Expression.Invoke(methodInfo, argsArrayExpr);
Expression.Invoke(instance, methodInfo, argsArrayExpr);
Expression.Invoke(methodEmitter, argsArrayExpr);
Expression.Invoke(instance, methodEmitter, argsArrayExpr);
Expression.Invoke(methodExpr, argsArrayExpr);          // 动态 MethodInfo 表达式
Expression.Invoke(instance, methodExpr, argsArrayExpr);
```

**`Call` vs `DeclaringCall`**：`Call` 对 virtual 方法发 `callvirt`（多态分派）；`DeclaringCall` 对非抽象非接口方法发 `call`（直接调用声明类型实现，绕过子类重写）。

---

### 10. 创建对象

```csharp
NewExpression New(Type instanceType);
NewExpression New(ConstructorInfo constructor);
NewExpression New(Type instanceType, params Expression[] parameters);
NewExpression New(ConstructorInfo constructor, params Expression[] parameters);
Expression    New(ConstructorEmitter constructorEmitter);
Expression    New(ConstructorEmitter constructorEmitter, params Expression[] parameters);
```

---

### 11. 对象初始化器

```csharp
MemberAssignment     Bind(MemberInfo member, Expression expression);
MemberInitExpression MemberInit(NewExpression newExpression, params MemberAssignment[] bindings);
MemberInitExpression MemberInit(NewExpression newExpression, IEnumerable<MemberAssignment> bindings);
```

**约束**：`Bind` 支持 `FieldInfo` 与 `PropertyInfo`（属性需有 setter）。

---

### 12. 数组

```csharp
NewArrayExpression    NewArray(int size);                              // new object[n]
NewArrayExpression    NewArray(int size, Type elementType);            // new T[n]
ArrayExpression       Array(params Expression[] arguments);            // new object[]{ ... }
ArrayExpression       Array(Type elementType, params Expression[] arguments);
ArrayIndexExpression  ArrayIndex(Expression array, int index);         // arr[i]，CanWrite=true
ArrayIndexExpression  ArrayIndex(Expression array, Expression index);  // arr[i]，CanWrite=true
ArrayLengthExpression ArrayLength(Expression array);                   // arr.Length
```

---

### 13. 二元运算

#### 算术（返回操作数同类型，支持 `op_Addition` 等用户重载）

| 方法 | 等价 |
|------|------|
| `Add(l, r)` | `l + r` |
| `AddChecked(l, r)` | `checked(l + r)`（`add.ovf`） |
| `AddAssign(l, r)` | `l += r` |
| `AddAssignChecked(l, r)` | `checked(l += r)` |
| `Subtract` / `SubtractChecked` / `SubtractAssign` / `SubtractAssignChecked` | 减法系列 |
| `Multiply` / `MultiplyChecked` / `MultiplyAssign` / `MultiplyAssignChecked` | 乘法系列 |
| `Divide(l, r)` | `l / r` |
| `DivideAssign(l, r)` | `l /= r` |
| `Modulo(l, r)` | `l % r` |
| `ModuloAssign(l, r)` | `l %= r` |

> `*Checked` 系列会发射 `add.ovf` / `sub.ovf` / `mul.ovf`，溢出时抛 `OverflowException`。仅在需要溢出检测时使用。

#### 比较（返回 `bool`）

| 方法 | 等价 |
|------|------|
| `Equal(l, r)` | `l == r` |
| `NotEqual(l, r)` | `l != r` |
| `LessThan(l, r)` | `l < r` |
| `LessThanOrEqual(l, r)` | `l <= r` |
| `GreaterThan(l, r)` | `l > r` |
| `GreaterThanOrEqual(l, r)` | `l >= r` |

##### `Equal` / `NotEqual` 的查找顺序

`Equal` / `NotEqual` 不是简单地发射 `ceq`，而是按以下优先级择一发射，目的是让生成代码与 C# 编译器输出对齐，避免开发者手写一堆 `if (x == null) ...` 样板：

1. **算术原生类型**（数值 / 枚举 / 引用恒等）：直接发射 `ceq`（`NotEqual` 追加一次 `ldc.i4.0; ceq` 取反）。
2. **用户重载** `op_Equality` / `op_Inequality`：在 `leftType` 与 `rightType` 上各自查找静态方法，按 `call` 发射。
3. **可空类型组合**（仅 `Equal` / `NotEqual`）：见下方"可空类型比较"。
4. **`IEquatable<T>` 实现**：若 `leftType` 实现了 `IEquatable<rightType>`，发射 `callvirt leftType.Equals(rightType)`。
5. **重写的** `Equals(object)`：仅当声明类型不是 `object` 本身时启用，按 `callvirt` 发射；这一步使得派生类的 `Equals` 重写仍然生效（虚分派）。
6. **`object` vs `object`**：退化为 `object.ReferenceEquals(...)` 静态调用，等价于 `(object)x == (object)y` 的引用相等。
7. 都不满足 → `AstException("不支持{leftType}与{rightType}的{Equal/NotEqual}运算！")`。

`NotEqual` 走 `Equals` / `ReferenceEquals` 路径时，框架自动在末尾追加 `ldc.i4.0; ceq` 进行取反，不需要手动写 `Not`。

##### 可空类型比较 — `T?` 与 `T` 的三种组合

`Equal` / `NotEqual` 自动识别下列三种可空组合，并按 C# 提升语义生成 IL（仅当底层类型为算术/枚举类型时启用）：

| 形态 | 生成的语义 |
|------|-----------|
| `T? == T?` | `(left.GetValueOrDefault() == right.GetValueOrDefault()) && (left.HasValue == right.HasValue)`；两者都为 `null` 视为相等 |
| `T? == T` | `left.HasValue && (left.GetValueOrDefault() == right)` |
| `T == T?` | 反之亦然 |

`NotEqual` 在上述结果末尾自动取反。底层类型不一致（如 `int? == long?`）不会触发可空提升，会落入第 4-6 步常规查找。

#### 短路逻辑（返回 `bool`）

| 方法 | 等价 |
|------|------|
| `AndAlso(l, r)` | `l && r` |
| `OrElse(l, r)` | `l \|\| r` |

#### 位运算（返回操作数同类型）

| 方法 | 等价 |
|------|------|
| `And(l, r)` / `AndAssign(l, r)` | `l & r` / `l &= r` |
| `Or(l, r)` / `OrAssign(l, r)` | `l \| r` / `l \|= r` |
| `ExclusiveOr(l, r)` / `ExclusiveOrAssign(l, r)` | `l ^ r` / `l ^= r` |

---

### 14. 一元运算

| 方法 | 等价 | 说明 |
|------|------|------|
| `Not(body)` | `!x` 或 `~x` | 布尔逻辑反 / 整数按位补 |
| `IsFalse(body)` | `!x` | 仅用于布尔假值判断 |
| `Negate(body)` | `-x` | 取负 |
| `Increment(body)` | `x + 1` | **生成增量值，不写回变量** |
| `Decrement(body)` | `x - 1` | **生成减量值，不写回变量** |
| `IncrementAssign(body)` | `++x` | 写回变量，`body.CanWrite == true` |
| `DecrementAssign(body)` | `--x` | 写回变量，`body.CanWrite == true` |

> `Increment` vs `IncrementAssign` 是常见易错点：前者只读，后者写回。

#### `Nullable<T>` 提升语义（lifted unary）

当 `body.RuntimeType` 为 `Nullable<T>` 时，`Negate` / `Not` / `Increment` / `Decrement` / `IncrementAssign` / `DecrementAssign` 自动按 C# 提升规则生成 IL：

- 若源值 `HasValue == false`，结果保持 `null`；
- 否则取出 `GetValueOrDefault()`，在底层类型 `T` 上执行运算，再用 `new Nullable<T>(result)` 装回。

返回类型与 `body.RuntimeType` 相同（依旧是 `Nullable<T>`），不需要手动 `HasValue` 守卫。底层类型仍要满足非可空版本的约束（`Negate` 不能用于无符号整数；`Not` 仅作用于整数 / 布尔）。

**例外 — `IsFalse` 不接受 `bool?`**：C# 原生 `false` 运算符仅作用于 `bool`，因此 `Expression.IsFalse(bool?)` 在构造时即抛 `AstException`。需要对 `bool?` 做"是否为 false"判断时，请显式写 `Equal(x, Constant(false, typeof(bool?)))` 或先 `Convert` 到 `bool`。

---

## BlockExpression — 代码块

| 成员 | 类型 | 说明 |
|------|------|------|
| `Expression.Block()` | 工厂 | 创建独立空块 |
| `IsEmpty` | `bool` | 块内无任何表达式 |
| `IsClosed` | `bool` | 最后一条为 `Return` / `Goto` / `Break` / `Continue` / `Throw` |
| `Append(expression)` | `BlockExpression` | 追加表达式，返回自身 |

```csharp
var block = Expression.Block();
block.Append(Expression.Assign(varX, Expression.Constant(1)));
block.Append(Expression.IncrementAssign(varX));
method.Append(block);   // 一旦 Append 进父块，block 即只读
```

**自动 Pop**：若块本身 `IsVoid`，追加非 void 表达式时框架自动插入 `Nop` 弹栈，确保 IL 栈平衡。

---

## 标签与跳转

```csharp
Label label = Expression.Label();              // 创建 Goto 类型标签
method.Append(Expression.Label(label));        // 标记位置（LabelExpression）
method.Append(Expression.Goto(label));         // 跳转（GotoExpression）
```

`LabelKind`：

| 类型 | 来源 | 用途 |
|------|------|------|
| `Goto` | `Expression.Label()` | 手动无条件跳转 |
| `Break` | `LoopExpression` / `ForEachExpression` 内部 | 跳出循环 |
| `Continue` | `LoopExpression` / `ForEachExpression` 内部 | 跳回循环头 |
| `Return` | `MethodEmitter` 内部 | 方法出口 |

> `Break`/`Continue`/`Return` 标签由框架自动管理，开发者只需创建 `Goto` 类型标签。

---

## Return — 方法返回

```csharp
Expression.Return();              // void 方法
Expression.Return(valueExpr);     // 有返回值
```

**约束**：必须出现在 `MethodEmitter` 的作用域链内。框架在 Emit 阶段自动绑定到方法出口，发射 `br` 跳转到 `ret` 之前。

---

## Loop — 循环

```csharp
LoopExpression loop = Expression.Loop();    // while(true){ ... }

loop.Append(Expression.IfThen(
    Expression.Equal(counter, Expression.Constant(10)),
    Expression.Break()));
loop.Append(Expression.IncrementAssign(counter));
loop.Append(Expression.Continue());          // 可省略，循环末尾自动跳回头部

method.Append(loop);
```

**约束**：

- `loop.IsEmpty` 时调用 `Load` 抛 `AstException`；
- `BreakExpression` / `ContinueExpression` 仅可出现在某个循环块的 `Append` 链中；对 `LoopExpression` 而言即当前 `loop` 内部。

---

## ForEach — 统一遍历循环

```csharp
var sum = Expression.Variable(typeof(int));
var item = Expression.Variable(typeof(int));

method.Append(Expression.Assign(sum, Expression.Constant(0)));

ForEachExpression loop = Expression.ForEach(item, sourceExpr);
loop.Append(Expression.AddAssign(sum, item));

method.Append(loop);
method.Append(sum);
```

### 签名

```csharp
ForEachExpression ForEach(VariableExpression item, Expression source)
```

### 选择策略

`Expression.ForEach(item, source)` 会按以下优先级决定生成哪类循环：

1. **索引循环（优先于枚举器）**
  - `source` 为一维数组；或
  - `source` 具备 `int Count` / `int Length` 属性以及 `this[int]` 索引器，且索引器返回类型与 `item.RuntimeType` 完全一致。
2. **泛型枚举器循环**
  - 若索引循环不适用，则优先查找返回元素类型与 `item.RuntimeType` 完全一致的 `IEnumerable<T>`。
3. **非泛型枚举器循环**
  - 仅当 `source` 不暴露任何 `IEnumerable<T>`、但实现了 `IEnumerable` 时才回退；此时每轮迭代会把 `IEnumerator.Current` 强转为 `item.RuntimeType`。

### 类型约束

- 数组路径要求 `item.RuntimeType == elementType`，否则构造时抛 `AstException`。
- 索引器路径要求 `this[int]` 返回类型与 `item.RuntimeType` 完全一致。
- 若 `source` 已暴露某个 `IEnumerable<T>`，但不存在 `T == item.RuntimeType` 的精确匹配，则**不会**降级到 `IEnumerable` 路径，而是直接抛 `AstException`。
- 非泛型 `IEnumerable` 路径下：
  - `item.RuntimeType == typeof(object)` 时直接赋值；
  - 值类型生成 `unbox.any`；
  - 引用类型生成 `castclass`，运行时不匹配将抛标准类型转换异常。

### 控制流语义

- `ForEachExpression` 继承自 `BlockExpression`，通过 `Append(...)` 追加循环体。
- 支持在循环体内部使用 `Expression.Break()` 与 `Expression.Continue()`。
- 与 `LoopExpression` 一样，仅允许 `Return` 标签向外冒泡。

### 示例

```csharp
var item = Expression.Variable(typeof(int));
var loop = Expression.ForEach(item, numbersExpr);

loop.Append(Expression.IfThen(
   Expression.Equal(item, Expression.Constant(0)),
   Expression.Continue()));

loop.Append(Expression.IfThen(
   Expression.GreaterThan(item, Expression.Constant(100)),
   Expression.Break()));

loop.Append(Expression.AddAssign(total, item));
```

---

## Switch — 分支

`SwitchExpression` 在**构造时**根据 `switchValue` 类型确定模式，之后不可切换。

### 模式 A：算术值比较（整数 / 枚举 / `char`）

```csharp
var sw = Expression.Switch(counter);                  // 无 default
var sw = Expression.Switch(counter, defaultBodyExpr); // 有 default

sw.Case(Expression.Constant(1))
  .Append(Expression.Call(method1))
  .Append(Expression.Break());        // 不加 Break 则 fallthrough

sw.Case(Expression.Constant(2))
  .Append(Expression.Call(method2));

method.Append(sw);
```

### 模式 B：引用类型匹配（`is T`，类似 C# 8 模式 switch）

```csharp
var sw = Expression.Switch(animalExpr);

var dogVar = Expression.Variable(typeof(Dog));   // 匹配成功后自动赋值
sw.Case(dogVar).Append(Expression.Call(dogVar, barkMethod));

var catVar = Expression.Variable(typeof(Cat));
sw.Case(catVar).Append(Expression.Call(catVar, meowMethod));

method.Append(sw);
```

> **不可混用**：`Case(ConstantExpression)` 与 `Case(VariableExpression)` 不能在同一 `SwitchExpression` 中混用，否则抛 `AstException`。

---

## Try-Catch-Finally — 异常处理

`TryExpression` 继承自 `BlockExpression`，`try` 块内容用 `Append`，`catch` / `finally` 用专用方法。

```csharp
// try { } finally { ... }
var t1 = Expression.Try(finallyBodyExpr);
t1.Append(riskyOperation);

// try { } catch(Exception) { ... }
var t2 = Expression.Try();
t2.Append(riskyOperation);
t2.Catch().Append(Expression.Call(logMethod));

// catch 指定异常类型
t2.Catch(typeof(InvalidOperationException))
  .Append(recoveryExpr);

// catch 并绑定到变量（catch(InvalidOperationException ex)）
var ex = Expression.Variable(typeof(InvalidOperationException));
t2.Catch(ex)
  .Append(Expression.Property(ex, typeof(Exception).GetProperty("Message")))
  .Append(Expression.Call(logExMethod, ex));

method.Append(t2);
```

**API 速查**：

| 方法 | 说明 |
|------|------|
| `Expression.Try()` | 无 finally |
| `Expression.Try(Expression finallyExpr)` | 带 finally |
| `tryExpr.Catch()` | 捕获 `Exception` |
| `tryExpr.Catch(Type)` | 捕获指定异常类型 |
| `tryExpr.Catch(VariableExpression)` | 捕获并赋值给变量（变量类型须继承 `Exception`） |
| `IErrorHandler.Append(expr)` | 向 catch 块追加；返回自身以链式调用 |

---

## 完整示例：生成 AOP 代理方法

> 目标等价 C#：
>
> ```csharp
> public class Proxy : CalculatorBase
> {
>     private readonly ICalculator _impl;
>     public Proxy(ICalculator impl) { _impl = impl; }
>     public override int Add(int a, int b)
>     {
>         try { return _impl.Add(a, b); }
>         catch (InvalidOperationException ex) { throw ex; }
>     }
> }
> ```

```csharp
var module = new ModuleEmitter("MyProxy");
var cls = module.DefineType(
    "Proxy.Calculator",
    TypeAttributes.Public | TypeAttributes.Class,
    typeof(CalculatorBase));

// 字段
var implField = cls.DefineField("_impl", typeof(ICalculator), FieldAttributes.Private);

// 构造函数
var ctor = cls.DefineConstructor(MethodAttributes.Public);
var implParam = ctor.DefineParameter(typeof(ICalculator), ParameterAttributes.None, "impl");
ctor.Append(Expression.Assign(implField, implParam));

// 重写 Add
var addMethod = cls.OverrideMethod(typeof(CalculatorBase).GetMethod("Add"));
var paramA = addMethod.GetParameters()[0];
var paramB = addMethod.GetParameters()[1];

// 注意：Expression.Throw(expr) 要求 expr.RuntimeType 严格继承自 Exception，
// 因此变量类型不能直接用 typeof(Exception)；如需 catch-all，可改用 .Catch(typeof(Exception))。
var ex = Expression.Variable(typeof(InvalidOperationException));
var tryExpr = Expression.Try();
tryExpr.Append(Expression.Return(
    Expression.Call(implField, typeof(ICalculator).GetMethod("Add"), paramA, paramB)));
tryExpr.Catch(ex).Append(Expression.Throw(ex));

addMethod.Append(tryExpr);

Type proxyType = cls.CreateType();
```

---

## DynamicMethod — 动态方法包装

`DynamicMethod` 继承自 `MethodInfo`，用于包装在动态类型中生成的方法，提供与反射兼容的 `MethodInfo` 接口。

| 属性 | 类型 | 说明 |
|------|------|------|
| `RuntimeMethod` | `MethodInfo` | 实际运行时方法（通常为 `MethodBuilder`） |

`DynamicMethod` 由框架内部自动创建，用户一般无需直接构造。当通过 `MethodEmitter.MakeGenericMethod(...)` 获取泛型实例化版本，或通过 `AbstractTypeEmitter.OverrideMethod(...)` 获取重写方法时，返回的即是 `DynamicMethod` 实例。

`Expression.Call(DynamicMethod, args)` 与 `Expression.Call(instance, DynamicMethod, args)` 会将其作为普通 `MethodInfo` 处理，自动选择 `call` / `callvirt` 指令。

---

## 行为约束 / 常见陷阱（Cheatsheet）

| 事项 | 规则 |
|------|------|
| `BlockExpression` 只读 | 一旦被 `Append` 进父块，自身变只读，再追加抛 `AstException`。需要复用应先构建完毕再嵌入 |
| `VariableExpression` 跨方法 | 同一实例只能在一个 `MethodEmitter` 中使用；每个方法体独立 `Expression.Variable(...)` |
| 泛型方法调用 | 调用前必须 `MakeGenericMethod`，否则 `MethodCallExpression` 构造时抛异常 |
| `Return` 位置 | 仅可出现在 `MethodEmitter` 作用域链；框架自动绑定出口标签 |
| `Switch` 模式 | 构造即决定，`Case(ConstantExpression)` / `Case(VariableExpression)` 不可混用 |
| 溢出检查 | 仅 `*Checked` 系列发射 `*.ovf` 指令；其它算术不做溢出检测 |
| `Call` vs `DeclaringCall` | virtual 方法 `Call` 发 `callvirt`（多态）；`DeclaringCall` 发 `call`（绑定声明类型） |
| `Increment` vs `IncrementAssign` | 前者**不写回**变量，仅产生新值；后者**写回**，要求 `CanWrite == true` |
| 赋值类型兼容 | 见第 5 节四条规则，任一满足即可 |
| `Coalesce` 类型 | 左侧必须为引用类型或 `Nullable<T>` |
| `Condition` 三目 | **必须有返回值**，void 分支或 `returnType=void` 抛 `AstException`，提示使用 `IfThenElse` |
| `IfThen` / `IfThenElse` | **均为 void**，仅用于流程控制，不可作为返回值表达式；需返回值用 `Condition` |
| `DeclaringCall` base 调用 | 获取基类 `MethodInfo` 后通过 `DeclaringCall` 发射 `call` 指令，绕过子类重写 |
| 自定义特性 | 类型/方法/字段/属性均支持 `SetCustomAttribute` / `DefineCustomAttribute`，泛型快捷方式 `SetCustomAttribute<MyAttr>()` |
| `ConstructorEmitter` 基类调用 | 使用 `InvokeBaseConstructor(ConstructorInfo, params Expression[])` 而非 `Expression.Base(...)` |
| `Equal` / `NotEqual` 回退链 | 算术 `ceq` → 用户 `op_Equality` → 可空提升 → `IEquatable<T>.Equals(T)`（`callvirt`）→ 重写的 `Equals(object)`（`callvirt`）→ `object.ReferenceEquals`（仅 `object` vs `object`）；都不满足才抛 `AstException` |
| `Nullable<T>` 自动提升 | `Equal` / `NotEqual` 与一元 `Negate` / `Not` / `Increment` / `Decrement` / `IncrementAssign` / `DecrementAssign` 自动按 lifted 语义生成 IL；`IsFalse(bool?)` 不支持 |
| Factory 校验位置 | 所有参数校验集中在 `Expression.Factory.cs` 的静态方法。**不要直接 `new XxxExpression(...)`**：会跳过校验，触发延迟 IL 错误 |

---

## 配套生态

- **[Inkslab.Intercept](https://www.nuget.org/packages/Inkslab.Intercept/)** — 基于本库构建的 AOP 拦截框架，通过返回值类型选择拦截器，零侵入接入 ASP.NET Core DI。

---

## 许可证

[MIT](LICENSE)
