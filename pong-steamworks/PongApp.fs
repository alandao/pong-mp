module PongApp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Lidgren.Network
open System.Collections.Generic


type PongClient () as x =
    inherit Game()

    do x.Content.RootDirectory <- ""


    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>
    let mutable dummyTexture = Unchecked.defaultof<Texture2D>

    let mutable isHosting = true
    let serverSocket = Server.StartSocket 12345
    let mutable clientsConnected:NetConnection list = []

    let clientSocket = Client.StartSocket "localhost" 12345

    let serverWorld = Server.defaultWorld
    let clientWorld = Client.defaultWorld

    let textures = Dictionary<string, Texture2D>()

    let entityPosition = Dictionary<Entity, Position>() |> EntityPosition
    let entityVelocity = Dictionary<Entity, Velocity>()
    let entityAppearance = Dictionary<Entity, Appearance>()

    override x.Initialize() =
        spriteBatch <- new SpriteBatch(x.GraphicsDevice)
        serverWorld.components.Add(entityPosition) |> ignore

        
        serverWorld.entities.Add("obstacle") |> ignore
        serverWorld.position.Add("obstacle", (Vector2(10.f, 60.f))) |> ignore
        serverWorld.velocity.Add("obstacle", (Vector2(10.f, 0.f))) |> ignore
        serverWorld.appearance.Add("obstacle", "obstacle") |> ignore
        serverWorld.entities.Add("obstacle1") |> ignore
        serverWorld.position.Add("obstacle1", (Vector2(10.f, 100.f))) |> ignore
        serverWorld.velocity.Add("obstacle1", (Vector2(5.f, 0.f))) |> ignore
        serverWorld.appearance.Add("obstacle1", "obstacle") |> ignore

        base.Initialize()

    override x.LoadContent() =
        dummyTexture <- new Texture2D(x.GraphicsDevice, 1, 1)
        dummyTexture.SetData([| Color.White |])

        textures.Add("obstacle", x.Content.Load<Texture2D> "obstacle.png")
        textures.Add("player", x.Content.Load<Texture2D> "player.png")

    override x.Update (gameTime) =
        let dt = gameTime.ElapsedGameTime.TotalSeconds
        if isHosting then
            //run serverside code
            clientsConnected <- Server.Update clientsConnected serverWorld dt serverSocket

        //clientside

        //update clientWorld
        Client.Update clientWorld clientSocket

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();
        // let playerInputs = getClientInputs
        
        //process player movement clientside
        //poll for new updates from server as fast as possible.
            

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue

        do spriteBatch.Begin ()
        Client.RunAppearance textures spriteBatch dummyTexture clientWorld

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