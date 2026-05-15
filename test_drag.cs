using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\sonsc\.nuget\packages\avalonia\11.0.10\lib\net7.0\Avalonia.Base.dll");
        var type = asm.GetType("Avalonia.Input.IDataObject");
        if (type != null) {
            foreach(var m in type.GetMethods()) Console.WriteLine("IDataObject." + m.Name);
        }
        type = asm.GetType("Avalonia.Input.IDataTransfer");
        if (type != null) {
            foreach(var m in type.GetMethods()) Console.WriteLine("IDataTransfer." + m.Name);
        }
    }
}
