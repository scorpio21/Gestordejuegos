using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        var asm = Assembly.LoadFrom(@"C:\Users\sonsc\.nuget\packages\avalonia\12.0.3\lib\net8.0\Avalonia.Base.dll");
        var type = asm.GetType("Avalonia.Input.IDataTransfer");
        if (type != null) {
            foreach(var m in type.GetMethods()) Console.WriteLine("IDataTransfer." + m.Name);
        }
        type = asm.GetType("Avalonia.Input.DataTransferExtensions");
        if (type != null) {
            foreach(var m in type.GetMethods()) Console.WriteLine("DataTransferExtensions." + m.Name);
        }
    }
}
