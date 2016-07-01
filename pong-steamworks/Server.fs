module Server

open Microsoft.Xna.Framework
open System.Collections.Generic
open System.Runtime.Serialization.Formatters.Binary
open Lidgren.Network

open HelperFunctions
open SharedServerClient


type World = {
    entities : HashSet<Entity>;
    sharedEntities : HashSet<Entity>;
   
    position: Dictionary<Entity, Position>;
    velocity: Dictionary<Entity, Velocity>;
    appearance: Dictionary<Entity, string>;
    }

let defaultWorld = {
    entities = HashSet<Entity>();
    sharedEntities = HashSet<Entity>();

    position = Dictionary<Entity, Position>();
    velocity = Dictionary<Entity, Velocity>();
    appearance = Dictionary<Entity, string>();
    }

let destroyEntity id world = 
    world.position.Remove(id) |> ignore
    world.velocity.Remove(id) |> ignore
    world.appearance.Remove(id) |> ignore
    world.entities.Remove(id) |> ignore

//  SYSTEMS

//updates entities with position and velocity
let private RunMovement (dt:float) world =
    let advance (pos:Position) (vel:Vector2) = ( pos + (float32 dt * vel) : Position)

    let entities = List<string>(world.position.Keys)
    for id in entities do
        let velocity = tryFind id world.velocity

        if Option.isSome velocity then
            world.position.[id] <- advance world.position.[id] (Option.get velocity) 
    

let private SendWorldToClients (world:World) clients (serverSocket:NetServer) =
    let message = serverSocket.CreateMessage()

    let binaryFormatter = new BinaryFormatter()
    let stream = new System.IO.MemoryStream()
    let netBuffer = new NetBuffer()

    ignore (world.sharedEntities.Add("fsdfg"))

    binaryFormatter.Serialize(stream, world.sharedEntities)

    //could possibly blow up if sharedentities is bigger than int32's max length.
    let bytesSharedEntities : byte [] = Array.zeroCreate (System.Convert.ToInt32 stream.Length)
    for i in 0..bytesSharedEntities.Length - 1 do
        bytesSharedEntities.[i] <- stream.GetBuffer().[i]

    netBuffer.Write(bytesSharedEntities.Length)
    netBuffer.Write(bytesSharedEntities)
    netBuffer.Write("Packet read okay!")
    message.Write(netBuffer)
    for client in clients do
        serverSocket.SendMessage(message, client, NetDeliveryMethod.Unreliable) |> ignore


//Public facing functions
let StartSocket port =
    let config = new NetPeerConfiguration("pong")
    config.Port <- port
    let server = new NetServer(config)
    server.Start()
    server

let Update (clients : NetConnection list) serverWorld dt (serverSocket:NetServer) =
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

    RunMovement dt serverWorld
            
    //send world to clients.
    SendWorldToClients serverWorld clients' serverSocket

    clients'