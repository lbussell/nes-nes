# Benchmarks

## Results

```
BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4652/24H2/2024Update/HudsonValley)
AMD Ryzen 7 9700X
.NET SDK 10.0.100-preview.6.25358.103
  [Host]     : .NET 10.0.0 (10.0.25.35903), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 10.0.0 (10.0.25.35903), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


| Method   | Mean     | Error     | StdDev    | Median   | Allocated |
|--------- |---------:|----------:|----------:|---------:|----------:|
| RunFrame | 1.098 ms | 0.0219 ms | 0.0617 ms | 1.126 ms |         - |
```

## How to run benchmarks

Set the `ROM` environment variable to the game that you want to benchmark. The
benchmark set up the console once and then step through a number of frames of
the game. Just run `dotnet run` in this directory to run the benchmark project.
