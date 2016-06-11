module PongApp

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open Lidgren.Network
open System.Collections.Generic

let StartServer port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server

let ServerSendMessageToClients (clients: NetConnection list) (serverSocket:NetServer) =
    let message = serverSocket.CreateMessage()
    if List.isEmpty clients then
        () // exit earlier since we don't need to send to any client.
    let mutable (clients' : List<NetConnection>) = new List<NetConnection>()
    List.iter (fun x -> clients'.Add(x)) clients
    message.Write("Server reporting in!")
    serverSocket.SendMessage(message, clients' , NetDeliveryMethod.Unreliable, 0)

let ServerRunFrame (clients : NetConnection list) serverWorld dt (serverSocket:NetServer) =
    let mutable clients' = clients

    //process messages from clients
    let mutable message = serverSocket.ReadMessage()
    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printfn "Server: message '%s' received" data
        
        | NetIncomingMessageType.StatusChanged ->
            //A client connected or disconnected.
            match message.SenderConnection.Status with
            | NetConnectionStatus.Connected ->
                //add client to connections list
                clients' <- message.SenderConnection::clients'
                printfn "Server: Client from %s has connected." (message.SenderEndPoint.ToString())
            | NetConnectionStatus.Disconnected ->
                //remove client from connections list
                clients' <- List.filter (fun x -> x <> message.SenderConnection) clients'
                printfn "Server: Client from %s has disconnected." (message.SenderEndPoint.ToString())
            | _ ->
                printfn "Server: Client from %s has an unhandled status." (message.SenderEndPoint.ToString())
           
        | NetIncomingMessageType.DebugMessage -> 
            ()
        | _ ->
            eprintfn "Server: Unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- serverSocket.ReadMessage()

    //retrieve inputs from clients
    //rawInput <- getInputfromClients

    //filter for inputs in appropriate context
    //input' <- filter inContext rawInput

    Server.runMovement dt serverWorld
            
    //send world to clients.
    ServerSendMessageToClients clients' serverSocket

    clients'

let StartClient (ip:string) port = 
    let config = new NetPeerConfiguration("pong")
    let client = new NetClient(config)
    client.Start()
    ignore(client.Connect(ip, port))
    client

let ClientRunFrame (world:Client.World) (clientSocket:NetClient) =
    let mutable message = clientSocket.ReadMessage()

    while message <> null do
        match message.MessageType with
        | NetIncomingMessageType.Data ->
            let data = message.ReadString()
            printfn "Client: Message received: %s" data

        | NetIncomingMessageType.StatusChanged -> ()
        | NetIncomingMessageType.DebugMessage -> ()
        | _ ->
            eprintfn "Client: Unhandled message with type: %s" (message.MessageType.ToString())
            ()
        message <- clientSocket.ReadMessage()

type PongClient () as x =
    inherit Game()

    do  x.Content.RootDirectory <- ""


    let graphics = new GraphicsDeviceManager(x)
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch>

    let mutable serverSocket = Unchecked.defaultof<NetServer>
    let mutable clientsConnected = []

    let mutable isHosting = true

    let mutable clientSocket = Unchecked.defaultof<NetClient>

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

        serverSocket <- StartServer 12345
        clientSocket <- StartClient "localhost" 12345

        base.Initialize()

    override x.LoadContent() =
        ()

    override x.Update (gameTime) =
        let dt = gameTime.ElapsedGameTime.TotalSeconds
        if isHosting then
            //serverside
            clientsConnected <- ServerRunFrame clientsConnected serverWorld dt serverSocket

        //clientside

        //update clientWorld
        ClientRunFrame clientWorld clientSocket

        if (Keyboard.GetState().IsKeyDown(Keys.Escape)) then
            x.Exit();
        // let playerInputs = getClientInputs
        
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