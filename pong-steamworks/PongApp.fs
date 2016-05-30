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



    do  x.Content.RootDirectory <- ""


    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>

    let mutable server = Unchecked.defaultof<NetServer>
    let mutable client = Unchecked.defaultof<NetClient>

    let mutable world = defaultWorld
    
    //debug stuff
    let mutable dummyTexture = Unchecked.defaultof<Texture2D>

    override x.Initialize() =
        spriteBatch <- new SpriteBatch(x.GraphicsDevice)

        server <- startServer 12345
        client <- startClient "localhost" 12345

        base.Initialize()

    override x.LoadContent() =
        dummyTexture <- new Texture2D(x.GraphicsDevice, 1, 1)
        dummyTexture.SetData([| Color.White |])
        world <- world |>
            createEntity "obstacle" |>
            addPosition "obstacle" (Vector2(10.f, 60.f)) |>
            addVelocity "obstacle" (Vector2(10.f, 0.f)) |>
            addAppearance "obstacle" "obstacle.png" x.Content |>
            createEntity "obstacle1" |>
            addPosition "obstacle1" (Vector2(10.f, 100.f)) |>
            addVelocity "obstacle1" (Vector2(5.f, 0.f)) |>
            addAppearance "obstacle1" "obstacle.png" x.Content |>
            createEntity "obstacle2" |>
            addPosition "obstacle2" (Vector2(10.f, 100.f)) |>
            addVelocity "obstacle2" (Vector2(2.5f, 0.f)) |>
            addAppearance "obstacle2" "obstacle.png" x.Content

    override x.Update (gameTime) =
        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();
                
        world <-  runMovement gameTime.ElapsedGameTime.TotalSeconds world
            

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue

        do spriteBatch.Begin ()
        runAppearance spriteBatch world

        //debug top left square which turns orange if game is running less than 60fps
        spriteBatch.Draw(dummyTexture, new Rectangle(0, 0, 20, 20), Color.Green)
        if gameTime.IsRunningSlowly then
            spriteBatch.Draw(dummyTexture, new Rectangle(0, 0, 20, 20), Color.OrangeRed)

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