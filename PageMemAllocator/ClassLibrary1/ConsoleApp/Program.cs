using System;
using CustomPageMemoryAllocator;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            MyPageMemoryAllocator allocator = new MyPageMemoryAllocator(1024);
            int ind1 = allocator.MemAllocate(24);
            allocator.WriteArray(ind1, new byte[] { 1, 4, 8, 5 });
            allocator.MemRealloc(ind1, 48);
            Console.WriteLine(allocator.MemDump());
            Console.ReadKey();
        }
    }
}
