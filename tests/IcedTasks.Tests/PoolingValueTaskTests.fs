namespace IcedTasks.Tests

open System
open Expecto
open System.Threading
open System.Threading.Tasks
open IcedTasks
#if !NETSTANDARD2_0

module PoolingValueTaskTests =

    let builderTests =
        testList "ValueTaskBuilder" [
            testList "Return" [
                testCaseAsync "Simple Return"
                <| async {
                    let foo = poolingValueTask { return "lol" }

                    let! result =
                        foo
                        |> Async.AwaitValueTask

                    Expect.equal result "lol" "Should be able to Return value"
                }
            ]
            testList "ReturnFrom" [
                testCaseAsync "Can ReturnFrom PoolingValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = poolingValueTask { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom PoolingValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected
                    let outerTask = poolingValueTask { return! fooTask }


                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }
                testCaseAsync "Can ReturnFrom Task"
                <| async {
                    let outerTask = poolingValueTask { return! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }
                testCaseAsync "Can ReturnFrom Task<T>"
                <| async {
                    let expected = "lol"
                    let outerTask = poolingValueTask { return! Task.FromResult expected }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected "Should be able to Return! value"
                }

                testCaseAsync "Can ReturnFrom TaskLike"
                <| async {
                    let fooTask = Task.Yield()
                    let outerTask = poolingValueTask { return! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is sufficient expect
                }

                testCaseAsync "Can ReturnFrom Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected
                    let outerTask = poolingValueTask { return! fooTask }


                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }
            ]

            testList "Binds" [
                testCaseAsync "Can Bind PoolingValueTask"
                <| async {
                    let fooTask: ValueTask = ValueTask.CompletedTask
                    let outerTask = poolingValueTask { do! fooTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }
                testCaseAsync "Can Bind PoolingValueTask<T>"
                <| async {
                    let expected = "lol"
                    let fooTask: ValueTask<_> = ValueTask.FromResult expected

                    let outerTask = poolingValueTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }
                testCaseAsync "Can Bind Task"
                <| async {
                    let outerTask = poolingValueTask { do! Task.CompletedTask }

                    do!
                        outerTask
                        |> Async.AwaitValueTask
                // Compiling is a sufficient Expect
                }

                testCaseAsync "Can Bind Task<T>"
                <| async {
                    let expected = "lol"

                    let outerTask = poolingValueTask {
                        let! result = Task.FromResult expected
                        return result
                    }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }


                testCaseAsync "Can Bind Async<T>"
                <| async {
                    let expected = "lol"
                    let fooTask = async.Return expected

                    let outerTask = poolingValueTask {
                        let! result = fooTask
                        return result
                    }

                    let! actual =
                        outerTask
                        |> Async.AwaitValueTask

                    Expect.equal actual expected ""
                }

            ]

            testList "Zero/Combine/Delay" [
                testCaseAsync "if statement"
                <| async {
                    let data = 42

                    let! actual =
                        poolingValueTask {
                            let result = data

                            if true then
                                ()

                            return result
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Zero/Combine/Delay should work"
                }
            ]

            testList "TryWith" [
                testCaseAsync "try with"
                <| async {
                    let data = 42

                    let! actual =
                        poolingValueTask {
                            let data = data

                            try
                                ()
                            with _ ->
                                ()

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "TryWith should work"
                }
            ]

            testList "TryFinally" [
                testCaseAsync "try finally"
                <| async {
                    let data = 42

                    let! actual =
                        poolingValueTask {
                            let data = data

                            try
                                ()
                            finally
                                ()

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "TryFinally should work"
                }
            ]

            testList "Using" [
                testCaseAsync "use IDisposable"
                <| async {
                    let data = 42

                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        poolingValueTask {
                            use d = TestHelpers.makeDisposable (doDispose)
                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IDisposable"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false
                    let doDispose () = wasDisposed <- true

                    let! actual =
                        poolingValueTask {
                            use! d =
                                TestHelpers.makeDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "use IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        poolingValueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IAsyncDisposable sync"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        wasDisposed <- true
                        ValueTask.CompletedTask

                    let! actual =
                        poolingValueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "use IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            Expect.isFalse wasDisposed ""
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask


                    let! actual =
                        poolingValueTask {
                            use d = TestHelpers.makeAsyncDisposable (doDispose)
                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }
                testCaseAsync "use! IAsyncDisposable async"
                <| async {
                    let data = 42
                    let mutable wasDisposed = false

                    let doDispose () =
                        task {
                            Expect.isFalse wasDisposed ""
                            do! Task.Yield()
                            wasDisposed <- true
                        }
                        |> ValueTask


                    let! actual =
                        poolingValueTask {
                            use! d =
                                TestHelpers.makeAsyncDisposable (doDispose)
                                |> async.Return

                            Expect.isFalse wasDisposed ""

                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                    Expect.isTrue wasDisposed ""
                }


                testCaseAsync "null"
                <| async {
                    let data = 42

                    let! actual =
                        poolingValueTask {
                            use d = null
                            return data
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual data "Should be able to use use"
                }
            ]


            testList "While" [
                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                poolingValueTask {
                                    while index < loops do
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual loops "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"while bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                poolingValueTask {
                                    while index < loops do
                                        do! Task.Yield()
                                        index <- index + 1

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual loops "Should be ok"
                        }
                    )
            ]

            testList "For" [

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for in {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                poolingValueTask {
                                    for i in [ 1..10 ] do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                poolingValueTask {
                                    for i = 1 to loops do
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )

                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind in {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                poolingValueTask {
                                    for i in [ 1..10 ] do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )


                yield!
                    [
                        10
                        10000
                        1000000
                    ]
                    |> List.map (fun loops ->
                        testCaseAsync $"for bind to {loops}"
                        <| async {
                            let mutable index = 0

                            let! actual =
                                poolingValueTask {
                                    for i = 1 to loops do
                                        do! Task.Yield()
                                        index <- i + i

                                    return index
                                }
                                |> Async.AwaitValueTask

                            Expect.equal actual index "Should be ok"
                        }
                    )
            ]
            testList "MergeSources" [
                testCaseAsync "and! 5"
                <| async {
                    let! actual =
                        poolingValueTask {
                            let! a = ValueTask.FromResult 1
                            and! b = Task.FromResult 2
                            and! c = coldTask { return 3 }
                            and! _ = Task.Yield()
                            and! _ = ValueTask.CompletedTask
                            // and! c = fun () -> PoolingValueTask.FromResult(3)
                            return a + b + c
                        }
                        |> Async.AwaitValueTask

                    Expect.equal actual 6 ""

                }
            ]
        ]


    // let asyncBuilderTests =
    //     testList "AsyncBuilder" [

    //         testCase "AsyncBuilder can Bind PoolingValueTask<T>"
    //         <| fun () ->
    //             let innerTask = poolingValueTask { return! poolingValueTask { return "lol" } }

    //             let outerAsync = async {
    //                 let! result = innerTask |> Async.AwaitValueTask
    //                 return result
    //             }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual "lol" ""


    //         testCase "AsyncBuilder can ReturnFrom PoolingValueTask<T>"
    //         <| fun () ->
    //             let innerTask = poolingValueTask { return! poolingValueTask { return "lol" } }

    //             let outerAsync = async { return! innerTask }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual "lol" ""

    //         testCase "AsyncBuilder can Bind PoolingValueTask"
    //         <| fun () ->
    //             let innerTask: PoolingValueTask = fun () -> Task.CompletedTask

    //             let outerAsync = async {
    //                 let! result = innerTask
    //                 return result
    //             }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual () ""

    //         testCase "AsyncBuilder can ReturnFrom PoolingValueTask"
    //         <| fun () ->
    //             let innerTask: PoolingValueTask = fun () -> Task.CompletedTask

    //             let outerAsync = async { return! innerTask }

    //             let actual = Async.RunSynchronously(outerAsync)
    //             Expect.equal actual () ""
    //     ]


    let functionTests =
        testList "functions" [
            testList "singleton" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ValueTask.singleton "lol"

                    let! someTask =
                        innerCall
                        |> Async.AwaitValueTask

                    Expect.equal "lol" someTask ""
                }
            ]
            testList "bind" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = poolingValueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> ValueTask.bind (fun x -> poolingValueTask { return x + "fooo" })
                        |> Async.AwaitValueTask

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "map" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = poolingValueTask { return "lol" }

                    let! someTask =
                        innerCall
                        |> ValueTask.map (fun x -> x + "fooo")
                        |> Async.AwaitValueTask

                    Expect.equal "lolfooo" someTask ""
                }
            ]
            testList "apply" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = poolingValueTask { return "lol" }
                    let applier = poolingValueTask { return fun x -> x + "fooo" }

                    let! someTask =
                        innerCall
                        |> ValueTask.apply applier
                        |> Async.AwaitValueTask

                    Expect.equal "lolfooo" someTask ""
                }
            ]

            testList "zip" [
                testCaseAsync "Simple"
                <| async {
                    let leftCall = poolingValueTask { return "lol" }
                    let rightCall = poolingValueTask { return "fooo" }

                    let! someTask =
                        ValueTask.zip leftCall rightCall
                        |> Async.AwaitValueTask

                    Expect.equal ("lol", "fooo") someTask ""
                }
            ]


            testList "ofUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ValueTask.CompletedTask

                    let! someTask =
                        innerCall
                        |> ValueTask.ofUnit
                        |> Async.AwaitValueTask

                    Expect.equal () someTask ""
                }
            ]


            testList "toUnit" [
                testCaseAsync "Simple"
                <| async {
                    let innerCall = ValueTask.FromResult "lol"

                    let! someTask =
                        innerCall
                        |> ValueTask.toUnit
                        |> Async.AwaitValueTask

                    Expect.equal () someTask ""
                }
            ]

        ]


    [<Tests>]
    let valueTaskTests =
        testList "IcedTasks.PoolingValueTask" [
            builderTests
            // asyncBuilderTests
            functionTests
        ]

#endif