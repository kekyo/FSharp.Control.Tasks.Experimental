# F# async computation infrastructure prove code

This is very experimental code fragment for F#'s combined async infrastructure.
This project goals:

* Invoking cost efficient.
  * Reduce binding cost.
  * Construct information types by value type.
* Main target for .NET standard.
  * Can handle Task<T>, Task, ValueTask<T> and Async<'T>.
* Can write more naturally computation expression than current AsyncBuilder impls.
* Send PR to FSharp.Core.
