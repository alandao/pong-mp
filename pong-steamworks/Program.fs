open PongClient

[<EntryPoint>]
let main argv =

    //initialize client
    use client = new PongClient()
    //statement below will block
    client.Run()

    printfn "Shutting Down"
    0 // return an integer exit code
