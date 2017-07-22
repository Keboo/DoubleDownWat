## Explanation 

At first glance, the code seems very counter intuative.
The order of operations looks like it should be:
* Invoke `Get<int>()`
* Invoke `GetInternal()`
* Increment `_invokeCount` and return its value.
* Since the return value is an `int` and it matches the generic parameter (T), cast it to an `int` and return it.

However since the unit test passes we know that `GetInternal()` is being invoked twice.
So, what is actually going on?

Examining the IL for this:
```C#
if (GetInternal() is T result)
```
Yields:
```IL
IL_0001: ldarg.0      // this
IL_0002: call         instance object DoubleDown.DoubleDown::GetInternal()
IL_0007: isinst       !!0/*T*/
IL_000c: brtrue.s     IL_0011
IL_000e: ldc.i4.0     
IL_000f: br.s         IL_001e
IL_0011: ldarg.0      // this
IL_0012: call         instance object DoubleDown.DoubleDown::GetInternal()
IL_0017: unbox.any    !!0/*T*/
IL_001c: stloc.0      // result
```

Right away we notice that there are two call instructions to `GetInternal()`.
Let's follow the IL through for our case to see what is happening.
`IL_0001-0002` invokes `this.GetInternal()` and puts the returned value on top of the stack.
`IL_0007` compares the values on top of the stack to see if the object on top of the stack is an instance of type T. If true (which it is in our case) it pushes an instance of the class on top of the stack, otherwise pushes a `null` on top of the stack.
`IL_000c` Branch to the target address if the value on top of the stack is non-zero. In our case this is true; so, we jump to `IL_0011`.
`IL_0011-IL_000012` invokes `this.GetInternal()` a second time and puts the returned value on top of the stack.
`IL_0017` Cast the value on top of the stack to the generic T type and push it onto the stack.
`IL_001c` Store the value on top of the stack in the `result` variable.

The issue is the unconstrained generic parameter coupled with the `is` operator.
Let's examine what happens when we constrain T to be a class.

```C#
public T Get<T>() where T : class
```
IL
```IL
IL_0001: ldarg.0      // this
IL_0002: call         instance object DoubleDown.DoubleDown::GetInternal()
IL_0007: isinst       !!0/*T*/
IL_000c: unbox.any    !!0/*T*/
```

And if we constrain T to be a struct (value type)
```C#
public T Get<T>() where T : class
```
IL
```
IL_0001: ldarg.0      // this
IL_0002: call         instance object DoubleDown.DoubleDown::GetInternal()
IL_0007: isinst       valuetype [mscorlib]System.Nullable`1<!!0/*T*/>
IL_000c: unbox.any    valuetype [mscorlib]System.Nullable`1<!!0/*T*/>
```

Because the inline `is` operator is effectively working like an `as` operator and assigning a variable. Because the `as` operator (or more specifically the `isinst` op code) act on reference types, it needs to know what _reference type_ to compare against (a `Nullable<T>` in the case of values types, or `T` for reference types).

Options for addressing the issue:

- Constrain the generic parameter.
- Declare an object variable before the `if` statement to force it to use a reference type (boxing will already have occured for reference types inside of the `GetInternal() method).
```C#
object internalValue = GetInternal();
if (internalValue is T result)
```
This addresses the problem by forcing the return value from `GetInternal()` into a reference type.

It is a bit surprising that the compiler does not simply generate a variable that matches the return type of `GetInternal()` (or whatever type the expression returns). Admittedly this would mean either additional logic ot determine if it is a method call, or simply do it always and be wasteful if the value being evaluated were a local variable, field, etc.
