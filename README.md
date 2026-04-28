![Inkslab](inkslab-mini.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/inkslab.expressions.svg)
![language](https://img.shields.io/github/languages/top/tinylit/inkslab.expressions.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/inkslab.expressions.svg)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab.expressions)](../../issues)

---

## 目录

- [概述](#概述)
- [NuGet 包](#nuget-包)
- [Inkslab.Expressions — 动态类型生成](#inkslabexpressions--动态类型生成)
  - [核心概念](#核心概念)
  - [快速上手](#快速上手)
  - [ModuleEmitter — 程序集模块](#moduleemitter--程序集模块)
  - [ClassEmitter — 动态类](#classemitter--动态类)
  - [MethodEmitter — 方法体](#methodemitter--方法体)
  - [ConstructorEmitter — 构造函数](#constructoremitter--构造函数)
  - [FieldEmitter / PropertyEmitter — 字段与属性](#fieldemitter--propertyemitter--字段与属性)
  - [EnumEmitter — 枚举](#enumemitter--枚举)
  - [Expression — 表达式系统](#expression--表达式系统)
- [Inkslab.Intercept — AOP 拦截框架](#inkslabintercept--aop-拦截框架)
  - [拦截器 Attribute 体系](#拦截器-attribute-体系)
  - [InterceptContext — 拦截上下文](#interceptcontext--拦截上下文)
  - [Intercept / InterceptAsync — 调用执行器](#intercept--interceptasync--调用执行器)
  - [注册与激活](#注册与激活)
  - [多拦截器链式执行](#多拦截器链式执行)
  - [NoninterceptAttribute — 跳过拦截](#noninterceptattribute--跳过拦截)
  - [完整使用示例](#完整使用示例)

---

## 概述

本仓库包含两个独立但配套的 NuGet 包：

| 包 | 职责 | 核心 API |
|----|------|----------|
| **Inkslab.Expressions** | 基于 `System.Reflection.Emit` 的动态类型生成器，提供类 Expression 树语法 | `ModuleEmitter`、`ClassEmitter`、`MethodEmitter`、`Expression` |
| **Inkslab.Intercept** | 基于方法返回值类型的 AOP 拦截框架，依赖 `Inkslab.Expressions` 在运行时生成代理类 | `InterceptAttribute`、`UseIntercept()`、`InterceptContext` |

> **设计思想**：`Inkslab.Expressions` 将底层 IL 指令封装为高可读的表达式 API，`Inkslab.Intercept` 则在其之上实现了零侵入的 AOP 能力。

---

## NuGet 包

```
PM> Install-Package Inkslab.Expressions
PM> Install-Package Inkslab.Intercept
```

| Package | NuGet | Downloads | 说明 |
| ------- | ----- | --------- | ---- |
| Inkslab.Expressions | [![Inkslab.Expressions](https://img.shields.io/nuget/v/Inkslab.Expressions.svg)](https://www.nuget.org/packages/Inkslab.Expressions/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Expressions) | 动态类型生成核心库 |
| Inkslab.Intercept | [![Inkslab.Intercept](https://img.shields.io/nuget/v/Inkslab.Intercept.svg)](https://www.nuget.org/packages/Inkslab.Intercept/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Intercept) | 基于方法返回值类型的 AOP 框架 |

---

## Inkslab.Expressions — 动态类型生成

### 核心概念

```
ModuleEmitter           ← 动态程序集/模块的根容器
  └── ClassEmitter      ← 定义一个动态类（继承 AbstractTypeEmitter）
        ├── FieldEmitter        ← 字段
        ├── PropertyEmitter     ← 属性
        ├── ConstructorEmitter  ← 构造函数
        └── MethodEmitter       ← 方法体（继承 BlockExpression）
              └── Expression.*  ← 表达式节点（赋值/条件/循环/调用...）
```

**生命周期**：`ModuleEmitter` → `DefineType` → 定义成员 → `CreateType()` → 使用运行时类型。

---

### 快速上手

```csharp
// 1. 创建模块（每个模块对应一个动态程序集）
var module = new ModuleEmitter("MyDynamicAssembly");

// 2. 定义类
var classEmitter = module.DefineType(
    "MyNamespace.Hello",
    TypeAttributes.Public | TypeAttributes.Class);

// 3. 定义字段
var nameField = classEmitter.DefineField("_name", typeof(string), FieldAttributes.Private);

// 4. 定义构造函数（接受 string 参数并赋值给字段）
var ctor = classEmitter.DefineConstructor(MethodAttributes.Public);
var nameParam = ctor.DefineParameter(typeof(string), ParameterAttributes.None, "name");
ctor.Append(Expression.Assign(nameField, nameParam));

// 5. 定义方法（直接返回字段值）
var greetMethod = classEmitter.DefineMethod("Greet", MethodAttributes.Public, typeof(string));
greetMethod.Append(nameField);

// 6. 编译并实例化
Type helloType = classEmitter.CreateType();
object instance = Activator.CreateInstance(helloType, "World");
string result = (string)helloType.GetMethod("Greet").Invoke(instance, null);
// result == "World"
```

---

### ModuleEmitter — 程序集模块

`ModuleEmitter` 是所有动态类型的容器，对应一个内存中的动态程序集。

```csharp
// 默认程序集名（Inkslab.Override）
var module = new ModuleEmitter();

// 自定义程序集名
var module = new ModuleEmitter("MyAssembly");

// 自定义名称 + 自定义文件名（.NET Framework 可保存到磁盘）
var module = new ModuleEmitter("MyAssembly", "MyAssembly.dll");

// 保存物理文件（仅 NET461+）
var module = new ModuleEmitter(savePhysicalAssembly: true);
```

| 方法 | 说明 |
|------|------|
| `DefineType(name, attributes)` | 定义公共或私有类，返回 `ClassEmitter` |
| `DefineType(name, attributes, baseType)` | 定义继承自 `baseType` 的类 |
| `DefineType(name, attributes, baseType, interfaces[])` | 定义实现多接口的类 |
| `DefineEnum(name, attributes, underlyingType)` | 定义枚举类型，返回 `EnumEmitter` |
| `SaveAssembly()` | 保存程序集到磁盘（仅 NET461+） |

> **常量**：`ModuleEmitter.DEFAULT_ASSEMBLY_NAME = "Inkslab.Override"`，`DEFAULT_FILE_NAME = "Inkslab.Override.dll"`

---

### ClassEmitter — 动态类

`ClassEmitter` 继承自 `AbstractTypeEmitter`，用于定义一个完整的动态 Class。

```csharp
// 无基类
var cls = module.DefineType("Foo.Bar", TypeAttributes.Public | TypeAttributes.Class);

// 继承基类
var cls = module.DefineType("Foo.Bar", TypeAttributes.Public | TypeAttributes.Class, typeof(MyBase));

// 继承基类 + 实现接口
var cls = module.DefineType("Foo.Bar",
    TypeAttributes.Public | TypeAttributes.Class,
    typeof(MyBase),
    new[] { typeof(IDisposable), typeof(ICloneable) });

// 完成定义后编译
Type runtimeType = cls.CreateType();
```

**`AbstractTypeEmitter` 定义成员的方法**：

| 方法 | 返回值 | 说明 |
|------|--------|------|
| `DefineField(name, type, attributes)` | `FieldEmitter` | 定义字段 |
| `DefineProperty(name, attributes, type)` | `PropertyEmitter` | 定义属性 |
| `DefineMethod(name, attributes, returnType)` | `MethodEmitter` | 定义方法 |
| `DefineConstructor(attributes)` | `ConstructorEmitter` | 定义构造函数 |
| `DefineNestedType(name, attributes)` | `ClassEmitter` | 定义嵌套类 |
| `DefineTypeInitializer()` | `TypeInitializerEmitter` | 定义静态构造函数 |
| `OverrideMethod(methodInfo)` | `MethodEmitter` | 重写/实现继承或接口方法 |

---

### MethodEmitter — 方法体

`MethodEmitter` 继承自 `BlockExpression`，支持在方法体内 `Append` 任意表达式节点。

```csharp
var method = classEmitter.DefineMethod(
    "Add",
    MethodAttributes.Public | MethodAttributes.Static,
    typeof(int));

var a = method.DefineParameter(typeof(int), ParameterAttributes.None, "a");
var b = method.DefineParameter(typeof(int), ParameterAttributes.None, "b");

// 方法体：return a + b;
method.Append(Expression.Add(a, b));
```

| 方法 | 说明 |
|------|------|
| `DefineParameter(type, attributes, name)` | 定义方法参数，返回 `ParameterEmitter`（可作为表达式使用） |
| `Append(expression)` | 向方法体追加一个表达式节点 |
| `MakeGenericMethod(typeArguments[])` | 获取泛型方法的实例化版本 |
| `SetCustomAttribute(builder)` | 添加自定义特性 |

---

### ConstructorEmitter — 构造函数

```csharp
var ctor = classEmitter.DefineConstructor(MethodAttributes.Public);
var param = ctor.DefineParameter(typeof(string), ParameterAttributes.None, "value");

// 调用基类构造函数（base(value)）
ctor.Append(Expression.Base(classEmitter, baseCtor, param));

// 为字段赋值
ctor.Append(Expression.Assign(fieldEmitter, param));
```

---

### FieldEmitter / PropertyEmitter — 字段与属性

```csharp
// 字段
var field = classEmitter.DefineField("_count", typeof(int), FieldAttributes.Private);

// 属性（含 getter/setter）
var backingField = classEmitter.DefineField("_name", typeof(string), FieldAttributes.Private);

var getter = classEmitter.DefineMethod("get_Name",
    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
    typeof(string));
getter.Append(backingField); // return _name;

var setter = classEmitter.DefineMethod("set_Name",
    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
    typeof(void));
var valueParam = setter.DefineParameter(typeof(string), ParameterAttributes.None, "value");
setter.Append(Expression.Assign(backingField, valueParam)); // _name = value;

var prop = classEmitter.DefineProperty("Name", PropertyAttributes.None, typeof(string));
prop.SetGetMethod(getter);
prop.SetSetMethod(setter);
```

`FieldEmitter` 本身实现了 `Expression`，可直接当表达式节点使用（读取字段值）。`PropertyEmitter` 同理，可参与赋值与读取表达式。

---

### EnumEmitter — 枚举

```csharp
var enumEmitter = module.DefineEnum("MyNamespace.Color", TypeAttributes.Public, typeof(int));
enumEmitter.DefineLiteral("Red",   0);
enumEmitter.DefineLiteral("Green", 1);
enumEmitter.DefineLiteral("Blue",  2);

Type colorType = enumEmitter.CreateType();
```

---

### Expression — 表达式系统

`Expression` 是所有表达式节点的抽象基类，所有节点**只能通过静态工厂方法创建**，不可直接 `new`。方法体（`MethodEmitter`）和构造函数体（`ConstructorEmitter`）本身继承自 `BlockExpression`，调用 `Append(expression)` 逐条追加语句，最后一条表达式的值即为方法返回值。

表达式覆盖了 C# 中常见的语句形式：

| 类别 | 代表方法 |
|------|---------|
| 值与变量 | `Constant`、`Default`、`Variable`、`Assign`、`This` |
| 控制流 | `IfThen`、`IfThenElse`、`Loop`、`ForEach`、`Switch`、`Break`、`Continue`、`Return` |
| 类型操作 | `Convert`、`TypeAs`、`TypeIs`、`Coalesce` |
| 方法与构造 | `Call`、`DeclaringCall`、`New`、`NewArray`、`ArrayIndex`、`MemberInit`、`Invoke` |
| 运算符 | `Add`、`Subtract`、`Multiply`、`Divide`、`Equal`、`AndAlso`、`OrElse`、`Not` 等 |
| 异常处理 | `Throw`、`TryCatch`、`TryCatchFinally`、`TryFinally` |

```csharp
// 示例：生成 "return a + b"
var method = classEmitter.DefineMethod("Add",
    MethodAttributes.Public | MethodAttributes.Static, typeof(int));
var a = method.DefineParameter(typeof(int), ParameterAttributes.None, "a");
var b = method.DefineParameter(typeof(int), ParameterAttributes.None, "b");
method.Append(Expression.Add(a, b)); // Append 最后一条表达式即为返回值
```

> 完整的工厂方法签名、节点继承关系、行为约束及二次封装注意事项，请参阅 **[Inkslab.Expressions.md](Inkslab.Expressions.md)**（同时也是 `Inkslab.Expressions` NuGet 包的 README）。其中循环相关能力现包含 `Expression.Loop()` 与统一的 `Expression.ForEach(...)`。

---

## Inkslab.Intercept — AOP 拦截框架

`Inkslab.Intercept` 在运行时为 DI 容器中的服务类型**自动生成代理子类**，通过方法标记的 `Attribute` 实现无侵入拦截。

### 拦截器 Attribute 体系

根据被拦截方法的**返回值类型**选择对应的基类：

| 基类 | 适用返回值类型 | 需重写的方法 |
|------|--------------|------------|
| `InterceptAttribute` | `void` | `void Run(InterceptContext, Intercept)` |
| `InterceptAsyncAttribute` | `Task` / `ValueTask` / `Task<T>` / `ValueTask<T>` | `Task RunAsync(InterceptContext, InterceptAsync)` |
| `ReturnValueInterceptAttribute` | `T` / `Task<T>` / `ValueTask<T>` | `T Run<T>(InterceptContext, Intercept<T>)` |
| `ReturnValueInterceptAsyncAttribute` | `Task<T>` / `ValueTask<T>` | `Task<T> RunAsync<T>(InterceptContext, InterceptAsync<T>)` |

> **继承关系**：`InterceptAsyncAttribute` 继承 `ReturnValueInterceptAsyncAttribute`；`ReturnValueInterceptAttribute` 继承 `ReturnValueInterceptAsyncAttribute`。
>
> **注意**：`T` 特指**非** `Task`/`ValueTask`/`Task<>`/`ValueTask<>` 的具体业务类型。若方法有返回值但标记的是 `InterceptAttribute`（`void` 拦截器），则该拦截器**不会生效**。

```csharp
// 示例：void 方法拦截器（记录日志）
public class LogInterceptAttribute : InterceptAttribute
{
    public override void Run(InterceptContext context, Intercept intercept)
    {
        Console.WriteLine($"[Before] {context.Main.Name}");
        intercept.Run(context); // 调用原始方法
        Console.WriteLine($"[After] {context.Main.Name}");
    }
}

// 示例：有返回值拦截器（结果缓存）
public class CacheInterceptAttribute : ReturnValueInterceptAttribute
{
    public override T Run<T>(InterceptContext context, Intercept<T> intercept)
    {
        var result = intercept.Run(context);
        // ... 缓存 result
        return result;
    }
}

// 示例：异步拦截器（耗时统计）
public class TimingInterceptAsyncAttribute : InterceptAsyncAttribute
{
    public override async Task RunAsync(InterceptContext context, InterceptAsync intercept)
    {
        var sw = Stopwatch.StartNew();
        await intercept.RunAsync(context);
        Console.WriteLine($"{context.Main.Name} took {sw.ElapsedMilliseconds}ms");
    }
}
```

---

### InterceptContext — 拦截上下文

拦截器方法执行时收到的上下文对象：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Services` | `IServiceProvider` | 当前请求的 DI 容器，可用于解析服务 |
| `Main` | `MethodInfo` | 被拦截的原始方法 |
| `Inputs` | `object[]` | 方法调用时传入的参数数组 |

```csharp
public override void Run(InterceptContext context, Intercept intercept)
{
    var logger = context.Services.GetRequiredService<ILogger<MyService>>();
    logger.LogInformation("调用方法: {Method}, 参数: {Args}",
        context.Main.Name,
        string.Join(", ", context.Inputs));

    intercept.Run(context);
}
```

---

### Intercept / InterceptAsync — 调用执行器

拦截器方法的第二个参数是"执行器"，调用它的 `Run` / `RunAsync` 即执行原始方法（或调用链中下一个拦截器）：

| 执行器类 | 方法 | 说明 |
|---------|------|------|
| `Intercept` | `void Run(InterceptContext)` | 执行 `void` 方法 |
| `Intercept<T>` | `T Run(InterceptContext)` | 执行有返回值方法 |
| `InterceptAsync` | `Task RunAsync(InterceptContext)` | 执行异步无返回值方法 |
| `InterceptAsync<T>` | `Task<T> RunAsync(InterceptContext)` | 执行异步有返回值方法 |

可通过继承执行器类并重写 `Run`/`RunAsync` 来定制执行行为（例如修改参数或替换返回值）。

---

### 注册与激活

**步骤一**：在接口或类的方法上标记拦截器 Attribute：

```csharp
public interface IOrderService
{
    [LogIntercept]
    void CreateOrder(OrderDto dto);

    [CacheIntercept]
    Task<Order> GetOrderAsync(int id);
}
```

**步骤二**：注册服务后调用 `UseIntercept()`：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // 先注册业务服务
    services.AddScoped<IOrderService, OrderServiceImpl>();

    // 激活拦截器（必须在所有需要拦截的服务注册之后调用）
    services.UseIntercept();
}
```

> `UseIntercept()` 会扫描此时已注册的所有 `ServiceDescriptor`，为服务类型上存在拦截 Attribute 的条目自动生成代理类并替换原始注册。**在 `UseIntercept()` 之后注册的服务不会被拦截**。

---

### 多拦截器链式执行

同一方法可标记多个拦截器 Attribute，框架通过 `MiddlewareIntercept` 按**标记顺序**依次执行（类似 ASP.NET Core 中间件管道）：

```csharp
[LogIntercept]      // 第 1 个执行
[TimingIntercept]   // 第 2 个执行
[CacheIntercept]    // 第 3 个执行（最内层，紧邻原始方法）
public virtual T GetData(int id) { ... }
```

执行顺序（洋葱模型）：

```
LogIntercept.Before
  → TimingIntercept.Before
    → CacheIntercept.Before
      → 原始方法
    → CacheIntercept.After
  → TimingIntercept.After
→ LogIntercept.After
```

---

### NoninterceptAttribute — 跳过拦截

在方法、属性、类或接口上标记 `[Nonintercept]`，可以阻止该目标被拦截：

```csharp
[Nonintercept] // 整个类不拦截
public class InternalService : IInternalService { ... }

public interface IMyService
{
    [Nonintercept] // 该方法不拦截
    void BypassMethod();

    [LogIntercept]
    void InterceptedMethod();
}
```

---

### 完整使用示例

```csharp
// 1. 定义拦截器
public class LogInterceptAttribute : InterceptAttribute
{
    public override void Run(InterceptContext context, Intercept intercept)
    {
        Console.WriteLine($">> {context.Main.Name}({string.Join(", ", context.Inputs)})");
        intercept.Run(context);
        Console.WriteLine($"<< {context.Main.Name}");
    }
}

public class RetryInterceptAsyncAttribute : InterceptAsyncAttribute
{
    public override async Task RunAsync(InterceptContext context, InterceptAsync intercept)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await intercept.RunAsync(context);
            }
            catch when (i < 2)
            {
                await Task.Delay(100);
            }
        }
    }
}

// 2. 在接口上标记
public interface IPaymentService
{
    [LogIntercept]
    void Pay(decimal amount);

    [RetryInterceptAsync]
    [LogIntercept]
    Task<bool> PayAsync(decimal amount);
}

// 3. 实现类（virtual 方法可被拦截；非 virtual 方法无法被拦截）
public class PaymentService : IPaymentService
{
    public virtual void Pay(decimal amount) { /* ... */ }
    public virtual async Task<bool> PayAsync(decimal amount) { /* ... */ }
}

// 4. 注册服务
services.AddScoped<IPaymentService, PaymentService>();
services.UseIntercept(); // 激活拦截器
```