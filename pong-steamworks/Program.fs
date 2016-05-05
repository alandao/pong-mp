// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open PongGame

[<EntryPoint>]
let main argv =
    use g = new Game1()
    g.Run()
    printfn "Hello World"
    0 // return an integer exit code
