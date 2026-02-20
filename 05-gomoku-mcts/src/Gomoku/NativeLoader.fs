module Gomoku.NativeLoader

open System.Runtime.InteropServices
open System.IO

let private load () =
    let exeDir = System.AppContext.BaseDirectory
    let nativeDir = Path.Combine(exeDir, "runtimes", "osx-arm64", "native")
    if Directory.Exists(nativeDir) then
        NativeLibrary.Load(Path.Combine(nativeDir, "libomp.dylib"))           |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libc10.dylib"))           |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libtorch_cpu.dylib"))     |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libtorch.dylib"))         |> ignore
        NativeLibrary.Load(Path.Combine(nativeDir, "libLibTorchSharp.dylib")) |> ignore

do load ()
