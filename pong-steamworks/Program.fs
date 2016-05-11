open PongClient
open Steamworks

[<EntryPoint>]
let main argv =
    let steamAPIInit = SteamAPI.Init()
    if not steamAPIInit then
        printfn "Error: SteamAPI_Init() failed"
        System.Environment.Exit 1

    let loggedIn = SteamUser.BLoggedOn()
    if not loggedIn then
        printfn "Error: Steam user isn't logged in"
        System.Environment.Exit 1

    //initialize client
    use client = new PongClient()
    //statement below will block
    client.Run()

    printfn "Shutting Down"
    SteamAPI.Shutdown()
    0 // return an integer exit code
