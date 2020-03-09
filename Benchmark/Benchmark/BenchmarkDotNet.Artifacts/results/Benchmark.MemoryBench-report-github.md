``` ini

BenchmarkDotNet=v0.12.0, OS=macOS 10.15.3 (19D76) [Darwin 19.3.0]
Intel Core i7-8850H CPU 2.60GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.102
  [Host]     : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT
  DefaultJob : .NET Core 3.1.2 (CoreCLR 4.700.20.6602, CoreFX 4.700.20.6702), X64 RyuJIT


```
| Method |     Mean |   Error |  StdDev |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------- |---------:|--------:|--------:|--------:|-------:|------:|----------:|
| Bench1 | 186.4 us | 1.19 us | 1.06 us | 24.1699 | 1.4648 |     - | 111.89 KB |
| Bench2 | 197.3 us | 2.81 us | 2.49 us | 10.7422 | 1.4648 |     - |  49.52 KB |
