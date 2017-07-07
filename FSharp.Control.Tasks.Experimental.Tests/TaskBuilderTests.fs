namespace FSharp.Control.TaskExtensions

open System
open System.Diagnostics
open System.Linq
open NUnit.Framework
open Microsoft.FSharp.Control

type private Disposable() =
    let record = new ResizeArray<string>()
    member __.Record = record |> Seq.toArray
    member __.Update() = record.Add("Update")
    interface IDisposable with
        member __.Dispose() = record.Add("Dispose")

[<TestFixture>]
type TaskBuilderTests() =

    //////////////////////////////////////////////////////////
    // zero, return, return!

    [<Test>]
    member __.ZeroTest() =
        let t = task {
            ignore()
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(Unchecked.defaultof<obj>, result)

    [<Test>]
    member __.ReturnTest() =
        let t = task {
            return 123
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(123, result)

    [<Test>]
    member __.ReturnFromTest() =
        let t = task {
            return! FSharpTask.FromResult 123
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(123, result)

    //////////////////////////////////////////////////////////
    // if, then

    [<Test>]
    member __.IfThenNonElseConditionIsTrueTest() =
        let value = 123
        let t = task {
            if value = 123 then
                return value + 1
            return 0
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(124, result)

    [<Test>]
    member __.IfThenNonElseConditionIsFalseTest() =
        let value = 124
        let t = task {
            if value = 123 then
                return value + 1
            return 0
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(0, result)

    //////////////////////////////////////////////////////////
    // let!, use

    [<Test>]
    member __.BindTest() =
        let t = task {
            let! value = FSharpTask.FromResult 123
            return value
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(123, result)

    [<Test>]
    member __.UsingTest() =
        let d = new Disposable()
        let t = task {
            use value = d
            d.Update()
            return 123
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(123, result)
        Assert.IsTrue(d.Record.SequenceEqual([|"Update";"Dispose"|]))

    //////////////////////////////////////////////////////////
    // while

    [<Test>]
    member __.WhileTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                remains <- remains - 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(55, sum)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)

    [<Test>]
    member __.CombineTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                remains <- remains - 1
            return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(55, sum)
        Assert.AreEqual(55, result)

    [<Test>]
    member __.WhileAndReturnAtFirstTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                return sum
                sum <- sum + remains
                remains <- remains - 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(0, sum)
        Assert.AreEqual(0, result)

    [<Test>]
    member __.WhileAndReturnAtMiddleTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                return sum
                remains <- remains - 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(10, sum)
        Assert.AreEqual(10, result)

    [<Test>]
    member __.WhileAndReturnAtLastTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                remains <- remains - 1
                return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(10, sum)
        Assert.AreEqual(10, result)

    [<Test>]
    member __.WhileAndReturnAtAfterAndExitTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                remains <- remains - 1
                return sum
            sum <- sum + 1
            return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(10, sum)
        Assert.AreEqual(10, result)

    [<Test>]
    member __.WhileAndReturnAtAfterAndNotExitTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                remains <- remains - 1
            sum <- sum + 1
            return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(56, sum)
        Assert.AreEqual(56, result)
          
    [<Test>]
    member __.WhileWithBindAtFirstTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                do! FSharpTask.Sleep 5.0
                sum <- sum + remains
                remains <- remains - 1
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(55, sum)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 50)
    
    [<Test>]
    member __.WhileWithBindAtMiddleTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                do! FSharpTask.Sleep 5.0
                remains <- remains - 1
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(55, sum)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 50)
    
    [<Test>]
    member __.WhileWithBindAtLastTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                remains <- remains - 1
                do! FSharpTask.Sleep 5.0
                ()  // TODO: 
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(55, sum)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 50)
    
    [<Test>]
    member __.WhileWithBindAndReturnBeforeTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                return sum
                do! FSharpTask.Sleep 5.0
                remains <- remains - 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(10, sum)
        Assert.AreEqual(10, result)

    [<Test>]
    member __.WhileWithBindAndReturnAfterTest() =
        let mutable sum = 0
        let t = task {
            let mutable remains = 10
            while remains > 0 do
                sum <- sum + remains
                do! FSharpTask.Sleep 10.0
                return sum
                remains <- remains - 1
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(10, sum)
        Assert.AreEqual(10, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 10)

    //////////////////////////////////////////////////////////
    // for

    [<Test>]
    member __.ForTest() =
        let mutable sum = 0
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(55, sum)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)

    [<Test>]
    member __.ForAndReturnAtFirstTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                return sum
                sum <- sum + value
                sum2 <- sum2 + 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(0, sum)
        Assert.AreEqual(100, sum2)
        Assert.AreEqual(0, result)

    [<Test>]
    member __.ForAndReturnAtMiddleTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                return sum
                sum2 <- sum2 + 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(1, sum)
        Assert.AreEqual(100, sum2)
        Assert.AreEqual(1, result)

    [<Test>]
    member __.ForAndReturnAtLastTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                sum2 <- sum2 + 1
                return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(1, sum)
        Assert.AreEqual(101, sum2)
        Assert.AreEqual(1, result)

    [<Test>]
    member __.ForAndReturnAtAfterAndExitTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                sum2 <- sum2 + 1
                return sum
            sum <- sum + 1
            return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(1, sum)
        Assert.AreEqual(101, sum2)
        Assert.AreEqual(1, result)

    [<Test>]
    member __.ForAndReturnAtAfterAndNotExitTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                sum2 <- sum2 + 1
            sum <- sum + 1
            return sum
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(56, sum)
        Assert.AreEqual(110, sum2)
        Assert.AreEqual(56, result)
 
          
    [<Test>]
    member __.ForWithBindAtFirstTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                do! FSharpTask.Sleep 5.0
                sum <- sum + value
                sum2 <- sum2 + 1
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(55, sum)
        Assert.AreEqual(110, sum2)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 50)
    
    [<Test>]
    member __.ForWithBindAtMiddleTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                do! FSharpTask.Sleep 5.0
                sum2 <- sum2 + 1
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(55, sum)
        Assert.AreEqual(110, sum2)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 50)
    
    [<Test>]
    member __.ForWithBindAtLastTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                sum2 <- sum2 + 1
                do! FSharpTask.Sleep 5.0
                ()  // TODO: 
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(55, sum)
        Assert.AreEqual(110, sum2)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 50)
    
    [<Test>]
    member __.ForWithBindAndReturnBeforeTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                return sum
                do! FSharpTask.Sleep 5.0
                sum2 <- sum2 + 1
        }

        let result = t |> FSharpTask.RunSynchronously
        Assert.AreEqual(1, sum)
        Assert.AreEqual(100, sum2)
        Assert.AreEqual(1, result)

    [<Test>]
    member __.ForWithBindAndReturnAfterTest() =
        let mutable sum = 0
        let mutable sum2 = 100
        let t = task {
            for value in Enumerable.Range(1, 10) do
                sum <- sum + value
                do! FSharpTask.Sleep 10.0
                return sum
                sum2 <- sum2 + 1
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(1, sum)
        Assert.AreEqual(100, sum2)
        Assert.AreEqual(1, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 10)

    //////////////////////////////////////////////////////////
    // try-finally

    [<Test>]
    member __.TryFinallyTest() =
        let mutable index = 0
        let t = task {
            try
                index <- index + 1
            finally
                index <- index + 10
        }

        let result = t |> FSharpTask.RunSynchronously

        Assert.AreEqual(11, index)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)

    [<Test>]
    member __.TryFinallyWithReturnAtFirstTest() =
        let mutable index = 0
        let t = task {
            try
                return 100
                index <- index + 1
            finally
                index <- index + 10
        }

        let result = t |> FSharpTask.RunSynchronously

        Assert.AreEqual(10, index)
        Assert.AreEqual(100, result)

    [<Test>]
    member __.TryFinallyWithReturnAtLastTest() =
        let mutable index = 0
        let t = task {
            try
                index <- index + 1
                return 100
            finally
                index <- index + 10
        }

        let result = t |> FSharpTask.RunSynchronously

        Assert.AreEqual(11, index)
        Assert.AreEqual(100, result)

    //////////////////////////////////////////////////////////
    // try-with



    //////////////////////////////////////////////////////////
    // ops

    [<Test>]
    member __.SleepTest() =
        let t = task {
            do! FSharpTask.Sleep 10.0
            return 123
        }

        let sw = new Stopwatch()
        sw.Start()

        let result = t |> FSharpTask.RunSynchronously

        sw.Stop()

        Assert.AreEqual(123, result)
        Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 10)

    [<Test>]
    member __.RunSynchronouslyTest() =
        let mutable index = 0
        let t = task {
            index <- index + 1
        }

        let result = t |> FSharpTask.RunSynchronously

        Assert.AreEqual(1, index)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)

        let result = t |> FSharpTask.RunSynchronously

        Assert.AreEqual(2, index)
        Assert.AreEqual(Unchecked.defaultof<obj>, result)
