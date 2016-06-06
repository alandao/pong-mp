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

let ServerRunFrame serverWorld dt (serverSocket:NetServer) =

    //process messages from clients
    let mutable message = serverSocket.ReadMessage()
    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printf "Server: message received: %s" data
        
        | NetIncomingMessageType.StatusChanged ->
            //A client connected or disconnected.
            ()
        | NetIncomingMessageType.DebugMessage -> 
            ()
        | _ ->
            eprintf "unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- serverSocket.ReadMessage()

    //retrieve inputs from clients
    //rawInput <- getInputfromClients

    //filter for inputs in appropriate context
    //input' <- filter inContext rawInput

    Server.runMovement dt serverWorld
            
    //send world to clients.
    

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
    let mutable message = clientSocket.ReadMessage()

    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printf "message received: %s" data

        | NetIncomingMessageType.StatusChanged -> ()
        | NetIncomingMessageType.DebugMessage -> ()
        | _ ->
            eprintf "unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- clientSocket.ReadMessage()

type PongClient () as x =
    inherit Game()

    do  x.Content.RootDirectory <- ""


    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>

    let mutable serverSocket = Unchecked.defaultof<NetServer>
    let mutable clientSocket = Unchecked.defaultof<NetClient>
    let mutable isHosting = true

    let serverWorld = Server.defaultWorld
    let clientWorld = Client.defaultWorld

    //debug stuff
    let mutable dummyTexture = Unchecked.defaultof<Texture2D>

    override x.Initialize() =
        spriteBatch <- new SpriteBatch(x.GraphicsDevice)

        
        serverWorld.entities.Add("obstacle") |> ignore
        serverWorld.position.Add("obstacle", (Vector2(10.f, 60.f))) |> ignore
        serverWorld.velocity.Add("obstacle", (Vector2(10.f, 0.f))) |> ignore
        serverWorld.entities.Add("obstacle1") |> ignore
        serverWorld.position.Add("obstacle1", (Vector2(10.f, 100.f))) |> ignore
        serverWorld.velocity.Add("obstacle1", (Vector2(5.f, 0.f))) |> ignore

        dummyTexture <- new Texture2D(x.GraphicsDevice, 1, 1)
        dummyTexture.SetData([| Color.White |])

        serverSocket <- startServer 12345
        clientSocket <- startClient "localhost" 12345

        base.Initialize()

    override x.LoadContent() =
        ()

    override x.Update (gameTime) =
        let dt = gameTime.ElapsedGameTime.TotalSeconds
        if isHosting then
            ServerRunFrame serverWorld dt serverSocket

        //clientside

        //update clientWorld

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();
//        let playerInputs = getClientInputs
        
        //process player movement clientside
        //poll for new updates from server as fast as possible.
            

    override x.Draw (gameTime) =
        x.GraphicsDevice.Clear Color.CornflowerBlue

        do spriteBatch.Begin ()
        Client.runAppearance spriteBatch clientWorld

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