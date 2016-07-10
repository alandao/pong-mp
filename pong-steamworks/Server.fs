module Server

open Microsoft.Xna.Framework
open System.Collections.Generic
open Lidgren.Network

open HelperFunctions
open SharedServerClient


type World = {
    entities : HashSet<Entity>;
    sharedEntities : HashSet<Entity>;

    components : Dictionary<System.Type, EntityComponentDictionary>;
    }

let defaultWorld = {
    entities = HashSet<Entity>();
    sharedEntities = HashSet<Entity>();

    components = Dictionary<System.Type, EntityComponentDictionary>();
    }

//  SYSTEMS

//updates entities with position and velocity
let private RunMovement (dt:float) (posComponents:Dictionary<Entity, Position>) velComponents =
    let advance (pos:Position) (vel:Vector2) = ( pos + (float32 dt * vel) : Position)

    let entities = List<string>(posComponents.Keys)
    for id in entities do
        let velocity = tryFind id velComponents

        if Option.isSome velocity then
            posComponents.[id] <- advance posComponents.[id] (Option.get velocity) 

//Clients will first receive a schema update before receiving world updates
let private SendSchemaToClients entities  clients (serverSocket:NetServer) =
    let mask x =
        let mutable buffer = 0
        if world.position.ContainsKey(x) then
            buffer <- buffer + int ComponentBit.bPosition
        if world.velocity.ContainsKey(x) then
            buffer <- buffer + int ComponentBit.bVelocity
        if world.appearance.ContainsKey(x) then
            buffer <- buffer + int ComponentBit.bAppearance
        
        if buffer = 0 then
            eprintfn "Server: Entity %s has no components!" x
            System.Diagnostics.Debugger.Launch() |> ignore
            System.Diagnostics.Debugger.Break()
        buffer

    let message = serverSocket.CreateMessage()
    let netBuffer = new NetBuffer()

    netBuffer.Write(world.sharedEntities.Count)
    for entity in world.sharedEntities do
        netBuffer.Write(entity)
        netBuffer.Write(mask entity)
    netBuffer.Write("Schema Update okay!")

    
let private SendWorldToClients (world:World) clients (serverSocket:NetServer) =

    //for each client, send a full snapshot of the gamestate
    for client in clients do
        let message = serverSocket.CreateMessage()
        let netBuffer = new NetBuffer()
        netBuffer.Write(world.sharedEntities.Count)
        for entity in world.sharedEntities do
            netBuffer.Write(entity)
            
        netBuffer.Write("World Update okay!")
        message.Write(netBuffer)
        serverSocket.SendMessage(message, client, NetDeliveryMethod.Unreliable) |> ignore



//Public facing functions
let StartSocket port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server

let Start port serverWorld dt (serverSocket:NetServer) =
    let mutable clients:NetConnection list = []

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

    RunMovement dt serverWorld
            
    //send world to clients.
    SendWorldToClients serverWorld clients' serverSocket

    clients'