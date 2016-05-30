module PongApp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open PongEntity
open ECS
open Lidgren.Network

let startServer port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server

let startClient (ip:string) port = 
    let config = new NetPeerConfiguration("pong")
    let client = new NetClient(config)
    client.Start()
    ignore(client.Connect(ip, port))
    client

type PongClient () as x =
    inherit Game()

    do x.Content.RootDirectory <- ""
    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>
    let mutable server = Unchecked.defaultof<NetServer>
    let mutable client = Unchecked.defaultof<NetClient>

    let CreateEntity' = CreateEntity x.Content
    let DrawEntity (sb:SpriteBatch) (entity:Entity) = 
        if entity.Texture.IsSome then
            do sb.Draw(entity.Texture.Value, entity.Position, Color.White)
        ()

    let WorldObjects = lazy ([("obstacle.png", Obstacle, Vector2(10.f,60.f), Vector2(32.f,32.f), true);]
                             |> List.map CreateEntity')

    let mutable world = defaultWorld

    override x.Initialize() =
        spriteBatch <- new SpriteBatch(x.GraphicsDevice)

        server <- startServer 12345
        client <- startClient "localhost" 12345

        base.Initialize()

    override x.LoadContent() = 
        //do WorldObjects.Force () |> ignore
        world <- world |>
            createEntity "obstacle" |>
            addPosition "obstacle" (Vector2(10.f, 60.f)) |>
            addAppearance "obstacle" "obstacle.png" x.Content
    override x.Update (gameTime) =
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();
        //
        
            
            

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue
        let DrawEntity' = DrawEntity spriteBatch
        do spriteBatch.Begin ()
        //WorldObjects.Value |> List.iter DrawEntity'
        runAppearance spriteBatch world
        do spriteBatch.End ()

    override x.UnloadContent() =
        ()

[<EntryPoint>]
let main argv =

    //initialize client
    use client = new PongClient()
    //statement below will block
    client.Run()

    printfn "Shutting Down"
    0 // return an integer exit code