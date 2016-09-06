module PongApp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Lidgren.Network
open System.Collections.Generic


type PongClient () as x =
    inherit Game()

    do x.Content.RootDirectory <- ""
    do x.Window.Title <- "Pong MP demo"

    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>
    let mutable dummyTexture = Unchecked.defaultof<Texture2D>

    let mutable isHosting = true
    let serverSocket = Server.StartSocket 12345
    let mutable clientsConnected:NetConnection list = []

    let clientSocket = Client.StartSocket "localhost" 12345

    override x.Initialize() =
        spriteBatch <- new SpriteBatch(x.GraphicsDevice)

        base.Initialize()

    override x.LoadContent() =
        dummyTexture <- new Texture2D(x.GraphicsDevice, 1, 1)
        dummyTexture.SetData([| Color.White |])

    override x.Update (gameTime) =
        let dt = gameTime.ElapsedGameTime.TotalSeconds
        if isHosting then
            //run serverside code

        //run clientside code

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue

        do spriteBatch.Begin ()

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