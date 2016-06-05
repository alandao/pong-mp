module PongApp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Lidgren.Network

open System.Collections.Generic

let startServer port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server

let sendMessageToClients (clients: NetConnection list) (serverSocket:NetServer) =
    let message = serverSocket.CreateMessage()
    let mutable (clients' : List<NetConnection>) = new List<NetConnection>()
    List.iter (fun x -> clients'.Add(x)) clients
    message.Write("hello world!")
    serverSocket.SendMessage(message, clients' , NetDeliveryMethod.Unreliable, 0)

let startClient (ip:string) port = 
    let config = new NetPeerConfiguration("pong")
    let client = new NetClient(config)
    client.Start()
    ignore(client.Connect(ip, port))
    client

let updateWorldFromServer (world:Client.World) (clientSocket:NetClient) =
    let message = clientSocket.ReadMessage()

    if message == null then world else
        
    match message.MessageType with
    | NetIncomingMessageType.Data ->
        let data = message.ReadString()
        printf "message received: %s" data
        world
    | NetIncomingMessageType.StatusChanged -> world
    | NetIncomingMessageType.DebugMessage -> world
    | _ ->
        eprintf "unhandled message with type: %s" (message.MessageType.ToString())
        world

type PongClient () as x =
    inherit Game()

    do  x.Content.RootDirectory <- ""


    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>

    let mutable server = Unchecked.defaultof<NetServer>
    let mutable client = Unchecked.defaultof<NetClient>
    let mutable isHosting = true

    let mutable serverWorld = Server.defaultWorld
    let mutable clientWorld = Client.defaultWorld

    //debug stuff
    let mutable dummyTexture = Unchecked.defaultof<Texture2D>

    override x.Initialize() =
        spriteBatch <- new SpriteBatch(x.GraphicsDevice)

        serverWorld <- serverWorld |>
            Server.createEntity "obstacle" |>
            Server.addPosition "obstacle" (Vector2(10.f, 60.f)) |>
            Server.addVelocity "obstacle" (Vector2(10.f, 0.f)) |>
            Server.createEntity "obstacle1" |>
            Server.addPosition "obstacle1" (Vector2(10.f, 100.f)) |>
            Server.addVelocity "obstacle1" (Vector2(5.f, 0.f)) |>
            Server.createEntity "obstacle2" |>
            Server.addPosition "obstacle2" (Vector2(10.f, 140.f)) |>
            Server.addVelocity "obstacle2" (Vector2(2.5f, 0.f))


        server <- startServer 12345
        client <- startClient "localhost" 12345

        base.Initialize()

    override x.LoadContent() =
        dummyTexture <- new Texture2D(x.GraphicsDevice, 1, 1)
        dummyTexture.SetData([| Color.White |])

            //addAppearance "obstacle1" "obstacle.png" x.Content |>

    override x.Update (gameTime) =
        if isHosting then
            //update server

            //retrieve inputs from clients
            //rawInput <- getInputfromClients

            //filter for inputs in appropriate context
            //input' <- filter inContext rawInput

            serverWorld <-  Server.runMovement gameTime.ElapsedGameTime.TotalSeconds serverWorld
            
            //send world to clients.


        //clientside

        //update clientWorld

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();
        let playerInputs = getClientInputs

        //process player movement clientside
        //poll for new updates from server as fast as possible.
            

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