open PongClient

[<EntryPoint>]
let main argv =
    use client = new PongClient()
    client.Run()
    printfn "Hello World"
    0 // return an integer exit code
