# VirtualMethodTableHook

```C#
class Program
{
    public virtual string MyToString() => "Hi";

    static unsafe void Main(string[] args)
    {
        VirtualTable virtualTable = new();

        var objectToString = typeof(object).GetMethod("ToString")!;
        var programMyMethod = typeof(Program).GetMethod("MyToString")!;

        virtualTable[objectToString] = programMyMethod;

        Console.WriteLine(new object().ToString()); // output Hi!
    }
}
```
