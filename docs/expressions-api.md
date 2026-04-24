# Expression API 参考

> 面向框架二次封装需求，覆盖所有表达式类型的完整签名与行为约束，可直接作为 Agent-Skills 或框架开发的参考规格。

---

## 目录

- [基类与核心属性](#基类与核心属性)
- [节点类继承关系](#节点类继承关系)
- [工厂方法完整速查](#工厂方法完整速查)
  - [常量与默认值](#常量与默认值)
  - [变量与参数](#变量与参数)
  - [this / base](#this--base)
  - [字段与属性](#字段与属性)
  - [赋值](#赋值)
  - [类型转换与检查](#类型转换与检查)
  - [三目运算](#三目运算)
  - [方法调用](#方法调用)
  - [创建对象](#创建对象)
  - [对象初始化器](#对象初始化器)
  - [数组](#数组)
  - [二元运算](#二元运算)
  - [一元运算](#一元运算)
- [BlockExpression — 代码块](#blockexpression--代码块)
- [标签与跳转](#标签与跳转)
- [Return — 方法返回](#return--方法返回)
- [Loop — 循环](#loop--循环)
- [Switch — 分支](#switch--分支)
- [Try-Catch-Finally — 异常处理](#try-catch-finally--异常处理)
- [封装示例：生成代理方法](#封装示例生成代理方法)
- [封装注意事项](#封装注意事项)

---

## 基类与核心属性

所有表达式均继承自抽象基类 `Expression`（命名空间 `Inkslab`）。

**基类关键属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `RuntimeType` | `Type` | 表达式的运行时返回类型 |
| `IsVoid` | `bool` | `RuntimeType == typeof(void)` |
| `IsContext` | `bool` | 是否为 `this` 上下文表达式 |
| `CanWrite` | `bool` | 是否可作为赋值左值（默认 `false`） |

**可写（`CanWrite == true`）的表达式类型：**

| 类型 | 说明 |
|------|------|
| `VariableExpression` | 局部变量 |
| `ParameterExpression` | 方法参数（非 `in` 只读参数） |
| `FieldExpression` | 非 `readonly` 字段 |
| `PropertyExpression` | 有 setter 的属性 |
| `ArrayIndexExpression` | 数组元素 |

---

## 节点类继承关系

```
Expression                        ← 抽象基类，命名空间 Inkslab
  ├── ConstantExpression          ← 常量
  ├── DefaultExpression           ← default(T)
  ├── ThisExpression              ← this / base
  ├── VariableExpression          ← 局部变量（CanWrite=true）
  │     └── ParameterExpression   ← 方法参数（CanWrite=true/false）
  ├── MemberExpression            ← 成员基类
  │     ├── FieldExpression       ← 字段（static 或实例）
  │     └── PropertyExpression    ← 属性（static 或实例）
  ├── BinaryExpression            ← 二元运算（算术/比较/逻辑/位运算）
  ├── UnaryExpression             ← 一元运算（递增/递减/取反/NOT）
  ├── ConvertExpression           ← (T)expr 强制转换
  ├── TypeIsExpression            ← expr is T → bool
  ├── TypeAsExpression            ← expr as T
  ├── CoalesceExpression          ← expr ?? fallback
  ├── ConditionExpression         ← test ? ifTrue : ifFalse
  ├── MethodCallExpression        ← obj.Method(args) / Type.Method(args)
  ├── InvocationExpression        ← MethodBase.Invoke(obj, object[])
  ├── NewExpression               ← new T(args)
  ├── NewArrayExpression          ← new T[n]
  ├── ArrayExpression             ← new T[]{ e0, e1, ... }
  ├── ArrayIndexExpression        ← arr[i]（CanWrite=true）
  ├── ArrayLengthExpression       ← arr.Length
  ├── MemberInitExpression        ← new T{ P = v, ... }
  ├── MemberAssignment            ← 成员绑定（用于 MemberInit）
  ├── ReturnExpression            ← return / return value
  ├── ThrowExpression             ← throw new Exception(...)
  ├── GotoExpression              ← goto label
  ├── LabelExpression             ← label: 标记点
  ├── BreakExpression             ← break（跳出 Loop）
  ├── ContinueExpression          ← continue（继续 Loop）
  └── BlockExpression             ← 代码块（顺序执行多个 Expression）
        ├── LoopExpression        ← while(true){...}（支持 Break/Continue）
        ├── IfThenExpression      ← if(test){...}
        ├── IfThenElseExpression  ← if(test){...}else{...}
        ├── SwitchExpression      ← switch(value){ case ... }
        ├── TryExpression         ← try{...}catch{...}finally{...}
        └── MethodEmitter         ← 方法体（继承 BlockExpression）
```

---

## 工厂方法完整速查

所有工厂方法均为 `Expression` 的静态方法。

### 常量与默认值

```csharp
// 从值推断类型（null → typeof(object)；MethodInfo → typeof(MethodInfo)；Type → typeof(Type)）
ConstantExpression Constant(object value)
ConstantExpression Constant(object value, Type constantType)

// default(T)
DefaultExpression Default(Type defaultType)
```

> `ConstantExpression.IsNull` 为 `true` 时表示 `null` 常量。支持值类型、引用类型、`Type` 对象、`MethodInfo` 对象。

---

### 变量与参数

```csharp
// 声明局部变量（仅声明，惰性分配 LocalBuilder）
VariableExpression Variable(Type variableType)

// 从 ParameterInfo 构建参数表达式（用于包装已有方法参数）
ParameterExpression Paramter(ParameterInfo parameter)
```

> `VariableExpression` 首次出现在 `Load(ILGenerator)` 中时才实际分配 `LocalBuilder`，因此同一实例可在不同方法体中复用**定义**，但不能跨方法体**使用**。

---

### this / base

```csharp
// 获取当前类型的 this 引用（不支持抽象类型）
Expression This(AbstractTypeEmitter typeEmitter)

// 在 ThisExpression 实例上获取 base 引用（不支持值类型）
var thisExpr = (ThisExpression)Expression.This(typeEmitter);
Expression baseExpr = thisExpr.Base;
```

---

### 字段与属性

```csharp
// 静态字段（field.IsStatic 必须为 true）
FieldExpression Field(FieldInfo field)
// 实例字段
FieldExpression Field(Expression instanceAst, FieldInfo field)

// 静态属性
PropertyExpression Property(PropertyInfo property)
// 实例属性
PropertyExpression Property(Expression instanceAst, PropertyInfo property)
```

> `FieldExpression` 和 `PropertyExpression` 均继承自 `MemberExpression`，`IsStatic` 可用于区分。
>
> `FieldEmitter` / `PropertyEmitter` 本身也实现了 `Expression`，可直接作为方法体的 `Append` 参数（读取值）或 `Assign` 的左值（写入值）。

---

### 赋值

```csharp
// x = y（left 必须 CanWrite == true）
Expression Assign(Expression left, Expression right)
```

赋值运算的类型检查规则（任意满足其一即通过）：

1. `IsAssignableFrom(left.RuntimeType, right.RuntimeType)` 为 `true`
2. `right` 为 `ThisExpression`
3. 两侧类型的枚举底层类型相同
4. `right.RuntimeType` 等于 `Nullable.GetUnderlyingType(left.RuntimeType)`

---

### 类型转换与检查

```csharp
ConvertExpression  Convert(Expression body, Type convertToType)  // (T)expr
TypeIsExpression   TypeIs(Expression body, Type bodyIsType)       // expr is T → bool
TypeAsExpression   TypeAs(Expression body, Type bodyAsType)       // expr as T
CoalesceExpression Coalesce(Expression left, Expression right)   // left ?? right
```

> - `Convert`：`body.RuntimeType == convertToType` 时为空操作（直接传值）。
> - `Coalesce`：要求 `left.RuntimeType` 为引用类型或可空值类型。

---

### 三目运算

```csharp
// test ? ifTrue : ifFalse（自动推断返回类型）
ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse)
// 显式指定返回类型
ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type returnType)
```

> `test.RuntimeType` 必须为 `typeof(bool)`。`ifTrue` 和 `ifFalse` 的类型须可隐式转换为 `returnType`，否则抛出 `ArgumentException`。

---

### 方法调用

框架提供三种方法调用方式，**语义不同**：

| 方法族 | IL 指令 | 参数传入方式 | 适用场景 |
|--------|---------|------------|---------|
| `Call` | `callvirt`（virtual）/ `call`（static/非virtual） | 逐个 `Expression` 参数 | 编译期已知签名的常规调用 |
| `DeclaringCall` | 接口/abstract → `callvirt`；其余 → `call` | 逐个 `Expression` 参数 | 显式调用声明类型实现，绕过子类重写 |
| `Invoke` | `MethodBase.Invoke(object, object[])` 反射调用 | 单个 `object[]` 表达式 | 运行期动态参数或动态方法 |

```csharp
// --- Call ---
Expression.Call(methodInfo)                                   // 静态，无参
Expression.Call(methodInfo, arg0, arg1)                       // 静态，有参
Expression.Call(instanceExpr, methodInfo)                     // 实例，无参
Expression.Call(instanceExpr, methodInfo, arg0, arg1)         // 实例，有参
Expression.Call(methodEmitter)                                // 同模块 MethodEmitter，无参
Expression.Call(methodEmitter, arg0, arg1)                    // 同模块 MethodEmitter，有参

// --- DeclaringCall ---
Expression.DeclaringCall(instanceExpr, methodInfo)
Expression.DeclaringCall(instanceExpr, methodInfo, arg0, arg1)

// --- Invoke（反射调用，参数为 object[] 表达式）---
Expression.Invoke(methodInfo, argsArrayExpr)                  // 静态方法
Expression.Invoke(instanceExpr, methodInfo, argsArrayExpr)    // 实例方法
Expression.Invoke(methodEmitter, argsArrayExpr)               // MethodEmitter 静态
Expression.Invoke(instanceExpr, methodEmitter, argsArrayExpr) // MethodEmitter 实例
Expression.Invoke(methodExpr, argsArrayExpr)                  // 动态 MethodInfo 表达式，静态
Expression.Invoke(instanceExpr, methodExpr, argsArrayExpr)    // 动态 MethodInfo 表达式，实例
```

---

### 创建对象

```csharp
// 无参构造函数
NewExpression New(Type instanceType)
NewExpression New(ConstructorInfo constructor)

// 有参构造函数
NewExpression New(Type instanceType, params Expression[] parameters)
NewExpression New(ConstructorInfo constructor, params Expression[] parameters)

// 同模块内 ConstructorEmitter
Expression New(ConstructorEmitter constructorEmitter)
Expression New(ConstructorEmitter constructorEmitter, params Expression[] parameters)
```

---

### 对象初始化器

```csharp
// new T { Member1 = expr1, Member2 = expr2 }
MemberAssignment    Bind(MemberInfo member, Expression expression)
MemberInitExpression MemberInit(NewExpression newExpression, params MemberAssignment[] bindings)
MemberInitExpression MemberInit(NewExpression newExpression, IEnumerable<MemberAssignment> bindings)
```

> `Bind` 支持 `FieldInfo` 和 `PropertyInfo`（属性需有 setter）。

---

### 数组

```csharp
// new object[n]
NewArrayExpression NewArray(int size)
// new T[n]
NewArrayExpression NewArray(int size, Type elementType)

// new object[]{ e0, e1, ... }
ArrayExpression Array(params Expression[] arguments)
// new T[]{ e0, e1, ... }
ArrayExpression Array(Type elementType, params Expression[] arguments)

// arr[i]（常量索引，CanWrite=true）
ArrayIndexExpression ArrayIndex(Expression array, int index)
// arr[i]（表达式索引，CanWrite=true）
ArrayIndexExpression ArrayIndex(Expression array, Expression index)

// arr.Length
ArrayLengthExpression ArrayLength(Expression array)
```

---

### 二元运算

#### 算术运算

返回类型与操作数相同；支持自定义运算符重载（`op_Addition` 等）。

| 方法 | C# 等价 | 说明 |
|------|---------|------|
| `Add(l, r)` | `l + r` | 加法 |
| `AddChecked(l, r)` | `checked(l + r)` | 加法，溢出检查（`add.ovf`） |
| `AddAssign(l, r)` | `l += r` | 加等于 |
| `AddAssignChecked(l, r)` | `checked(l += r)` | 加等于，溢出检查 |
| `Subtract(l, r)` | `l - r` | 减法 |
| `SubtractChecked(l, r)` | `checked(l - r)` | 减法，溢出检查（`sub.ovf`） |
| `SubtractAssign(l, r)` | `l -= r` | 减等于 |
| `SubtractAssignChecked(l, r)` | `checked(l -= r)` | 减等于，溢出检查 |
| `Multiply(l, r)` | `l * r` | 乘法 |
| `MultiplyChecked(l, r)` | `checked(l * r)` | 乘法，溢出检查（`mul.ovf`） |
| `MultiplyAssign(l, r)` | `l *= r` | 乘等于 |
| `MultiplyAssignChecked(l, r)` | `checked(l *= r)` | 乘等于，溢出检查 |
| `Divide(l, r)` | `l / r` | 除法 |
| `DivideAssign(l, r)` | `l /= r` | 除等于 |
| `Modulo(l, r)` | `l % r` | 取模 |
| `ModuloAssign(l, r)` | `l %= r` | 取模等于 |

#### 比较运算（返回 `bool`）

| 方法 | C# 等价 |
|------|---------|
| `Equal(l, r)` | `l == r` |
| `NotEqual(l, r)` | `l != r` |
| `LessThan(l, r)` | `l < r` |
| `LessThanOrEqual(l, r)` | `l <= r` |
| `GreaterThan(l, r)` | `l > r` |
| `GreaterThanOrEqual(l, r)` | `l >= r` |

#### 逻辑运算（返回 `bool`，短路求值）

| 方法 | C# 等价 | 说明 |
|------|---------|------|
| `AndAlso(l, r)` | `l && r` | 短路逻辑与 |
| `OrElse(l, r)` | `l \|\| r` | 短路逻辑或 |

#### 位运算（返回与操作数相同的类型）

| 方法 | C# 等价 |
|------|---------|
| `And(l, r)` | `l & r` |
| `AndAssign(l, r)` | `l &= r` |
| `Or(l, r)` | `l \| r` |
| `OrAssign(l, r)` | `l \|= r` |
| `ExclusiveOr(l, r)` | `l ^ r` |
| `ExclusiveOrAssign(l, r)` | `l ^= r` |

---

### 一元运算

| 方法 | C# 等价 | 说明 |
|------|---------|------|
| `Not(body)` | `!x` / `~x` | 逻辑反（bool）或按位补（整数） |
| `IsFalse(body)` | `!x` | 仅用于布尔假值判断 |
| `Negate(body)` | `-x` | 正负取反 |
| `Increment(body)` | `x + 1` | 生成增量值，**不修改变量** |
| `Decrement(body)` | `x - 1` | 生成减量值，**不修改变量** |
| `IncrementAssign(body)` | `++x` | 递增赋值（`body` 需 `CanWrite == true`） |
| `DecrementAssign(body)` | `--x` | 递减赋值（`body` 需 `CanWrite == true`） |

---

## BlockExpression — 代码块

`BlockExpression` 是方法体、循环体、分支体、异常块等所有多语句容器的基类。

```csharp
// 创建独立代码块
BlockExpression block = Expression.Block();
block.Append(Expression.Assign(varX, Expression.Constant(1)));
block.Append(Expression.IncrementAssign(varX));

// 嵌套进方法体
method.Append(block);
// 注意：block 一旦被 Append 进另一个块，就变为只读，之后再 Append 会抛 AstException
```

**关键属性与行为：**

| 成员 | 类型 | 说明 |
|------|------|------|
| `IsEmpty` | `bool` | 代码块中无任何表达式 |
| `IsClosed` | `bool` | 最后一条为 `Return`/`Goto`/`Break`/`Continue`/`Throw` |
| `Append(expr)` | `BlockExpression` | 追加表达式，返回自身（链式可用） |

> **自动 Pop 规则**：若 `BlockExpression` 本身为 `void` 块（`IsVoid == true`），追加非 void 表达式时框架会自动插入 `Nop`（弹出栈顶值），确保 IL 栈平衡。无需手动处理。

---

## 标签与跳转

```csharp
// 1. 创建标签对象（LabelKind.Goto）
Label label = Expression.Label();

// 2. 在代码中标记跳转目标位置
method.Append(Expression.Label(label));   // LabelExpression

// 3. 在代码中插入跳转指令
method.Append(Expression.Goto(label));    // GotoExpression
```

**`LabelKind` 说明：**

| 类型 | 来源 | 用途 |
|------|------|------|
| `Goto` | `Expression.Label()` | 手动无条件跳转 |
| `Break` | 框架内部（`LoopExpression`） | 跳出循环 |
| `Continue` | 框架内部（`LoopExpression`） | 跳回循环头 |
| `Return` | 框架内部（`MethodEmitter`） | 方法返回出口 |

框架自动管理 `Break`/`Continue`/`Return` 标签，开发者只需使用 `Expression.Label()` 创建 `Goto` 类型的标签。

---

## Return — 方法返回

```csharp
// void 方法
method.Append(Expression.Return());

// 有返回值
method.Append(Expression.Return(resultExpr));
```

> `ReturnExpression` 必须在 `MethodEmitter` 的作用域链内使用。框架在 `MethodEmitter.Emit` 阶段自动将其绑定到方法出口标签，生成 `br` 跳转到 `ret` 指令前。

---

## Loop — 循环

```csharp
// while(true) { ... }
LoopExpression loop = Expression.Loop();

loop.Append(Expression.IfThen(
    Expression.Equal(counter, Expression.Constant(10)),
    Expression.Break()    // break：跳出循环
));
loop.Append(Expression.IncrementAssign(counter));
loop.Append(Expression.Continue());               // continue：跳回循环头（可省略，自动跳回）

method.Append(loop);
```

**约束：**

| 约束 | 说明 |
|------|------|
| 非空 | `LoopExpression.IsEmpty == true` 时 `Load` 会抛 `AstException` |
| `Break` 作用域 | `BreakExpression` 只能出现在 `LoopExpression` 的 `Append` 链中；在 Loop 外使用会在运行时抛异常 |
| `Continue` 作用域 | 同上，`ContinueExpression` 只能用于 `LoopExpression` 内 |

---

## Switch — 分支

`SwitchExpression` 的工作模式在**构造时**由 `switchValue` 的类型决定，之后不可切换。

### 模式一：算术值比较

适用于 `switchValue.RuntimeType` 为整数、枚举、`char` 等算术类型。

```csharp
// 无 default
var sw = Expression.Switch(counter);
// 有 default（直接传入 default 分支的表达式）
var sw = Expression.Switch(counter, defaultBodyExpr);

// case 1:
sw.Case(Expression.Constant(1))
  .Append(Expression.Call(method1))
  .Append(Expression.Break());          // 不加 Break 则自动 fallthrough

// case 2:
sw.Case(Expression.Constant(2))
  .Append(Expression.Call(method2));

method.Append(sw);
```

### 模式二：引用类型匹配（`is T`）

适用于 `switchValue.RuntimeType` 为引用类型，相当于 C# 8+ 的模式匹配 `switch`。

```csharp
var sw = Expression.Switch(animalExpr);

// case Dog dog:（匹配成功时自动将转换结果赋值给变量）
var dogVar = Expression.Variable(typeof(Dog));
sw.Case(dogVar)
  .Append(Expression.Call(dogVar, barkMethod));

var catVar = Expression.Variable(typeof(Cat));
sw.Case(catVar)
  .Append(Expression.Call(catVar, meowMethod));

method.Append(sw);
```

> **禁止混用**：`Case(ConstantExpression)` 用于模式一，`Case(VariableExpression)` 用于模式二，**两者不能在同一个 `SwitchExpression` 中混用**，否则抛出 `AstException`。

---

## Try-Catch-Finally — 异常处理

`TryExpression` 继承自 `BlockExpression`，`try` 块的内容通过 `Append` 添加，`catch`/`finally` 通过专用方法配置。

```csharp
// try { } finally { ... }
var tryExpr = Expression.Try(finallyBodyExpr);
tryExpr.Append(riskyOperation);

// try { } catch(Exception) { ... }
var tryExpr = Expression.Try();
tryExpr.Append(riskyOperation);
tryExpr.Catch()                                // catch(Exception)
    .Append(Expression.Call(logMethod));

// catch 指定异常类型
tryExpr.Catch(typeof(InvalidOperationException))
    .Append(someRecovery);

// catch 并绑定到变量（相当于 catch(InvalidOperationException ex)）
var exVar = Expression.Variable(typeof(InvalidOperationException));
tryExpr.Catch(exVar)
    .Append(Expression.Property(exVar, typeof(Exception).GetProperty("Message")))
    .Append(Expression.Call(logExMethod, exVar));

method.Append(tryExpr);
```

**`TryExpression` 方法速查：**

| 方法 | 说明 |
|------|------|
| `Expression.Try()` | 创建无 finally 的 try 块 |
| `Expression.Try(Expression finallyExpr)` | 创建带 finally 的 try 块 |
| `tryExpr.Catch()` | 捕获所有 `Exception` |
| `tryExpr.Catch(Type exceptionType)` | 捕获指定异常类型 |
| `tryExpr.Catch(VariableExpression variable)` | 捕获并赋值给变量（变量类型须继承 `Exception`） |
| `catchHandler.Append(expr)` | 向 catch 块追加表达式；返回 `IErrorHandler`，支持链式调用 |

---

## 封装示例：生成代理方法

以下示例演示如何利用上述 API 生成"将接口方法调用转发到实现类"的动态代理方法：

```csharp
// 目标等价 C# 代码：
// public class Proxy : CalculatorBase {
//     private readonly ICalculator _impl;
//     public Proxy(ICalculator impl) { _impl = impl; }
//     public override int Add(int a, int b) {
//         try { return _impl.Add(a, b); }
//         catch (Exception ex) { throw; }
//     }
// }

var module = new ModuleEmitter("MyProxy");
var cls = module.DefineType(
    "Proxy.Calculator",
    TypeAttributes.Public | TypeAttributes.Class,
    typeof(CalculatorBase));

// 注入字段
var implField = cls.DefineField("_impl", typeof(ICalculator), FieldAttributes.Private);

// 构造函数
var ctor = cls.DefineConstructor(MethodAttributes.Public);
var implParam = ctor.DefineParameter(typeof(ICalculator), ParameterAttributes.None, "impl");
ctor.Append(Expression.Assign(implField, implParam));

// 重写 Add 方法
var addMethod = cls.OverrideMethod(typeof(CalculatorBase).GetMethod("Add"));
var paramA = addMethod.GetParameters()[0];
var paramB = addMethod.GetParameters()[1];

var exVar = Expression.Variable(typeof(Exception));
var tryExpr = Expression.Try();
tryExpr.Append(Expression.Return(
    Expression.Call(implField, typeof(ICalculator).GetMethod("Add"), paramA, paramB)));
tryExpr.Catch(exVar)
    .Append(Expression.Throw(exVar));

addMethod.Append(tryExpr);

Type proxyType = cls.CreateType();
```

---

## 封装注意事项

| 事项 | 说明 |
|------|------|
| `BlockExpression` 只读性 | 代码块一旦被 `Append` 进另一个块，就变为只读（`isReadOnly = true`），再次 `Append` 会抛 `AstException`。需要复用时应先构建完毕再嵌入 |
| 泛型方法调用 | 调用泛型方法前须先 `MakeGenericMethod`，否则含 `IsGenericParameter` 的类型参数会在 `MethodCallExpression` 构造时抛异常 |
| `VariableExpression` 跨方法体 | 同一 `VariableExpression` 实例不能用于两个不同的 `MethodEmitter`，每个方法体应创建独立实例 |
| `Return` 的位置 | `ReturnExpression` 只能出现在 `MethodEmitter` 作用域链内；框架在 `Emit` 时自动绑定返回标签，无需手动管理 |
| `Switch` 模式不可切换 | `SwitchExpression` 的模式（算术比较 vs 类型匹配）在构造时确定，`Case(ConstantExpression)` 与 `Case(VariableExpression)` 不能混用于同一实例 |
| 溢出检查变体 | `AddChecked`/`SubtractChecked`/`MultiplyChecked`（及其 `Assign` 版本）生成 `add.ovf`/`sub.ovf`/`mul.ovf` 指令；仅在需要运行时溢出检测时使用 |
| `DeclaringCall` vs `Call` | `Call` 对 virtual 方法生成 `callvirt`（多态分派）；`DeclaringCall` 对非抽象非接口方法生成 `call`（直接调用声明类型实现，绕过子类重写） |
| `Increment` vs `IncrementAssign` | `Increment(x)` 生成 `x + 1` 的值但**不写回变量**；`IncrementAssign(x)` 等价 `++x`，**写回变量**（需 `CanWrite == true`） |
