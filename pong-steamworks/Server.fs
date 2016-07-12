module Server

open Microsoft.Xna.Framework
open System.Collections.Generic
open Lidgren.Network

open HelperFunctions
open SharedServerClient
open ECS
open ECSTypes


//  SYSTEMS

//Clients will first receive a schema update before receiving world updates 
let private SendSchemaToClients entityManager clients (serverSocket:NetServer) =
    let message = serverSocket.CreateMessage()
    let netBuffer = new NetBuffer()
    
    netBuffer.Write(byte ServerMessage.Schema)
    netBuffer.Write(entityManager.entities.Count)
    for entID in entityManager.entities do
        netBuffer.Write(entID.ToByteArray())
        netBuffer.Write(ComponentMask entID entityManager)
    netBuffer.Write("Schema Update okay!")
    message.Write(netBuffer)

    for client in clients do
        serverSocket.SendMessage(message, client, NetDeliveryMethod.Unreliable) |> ignore

    
let private SendGameStateToClients (world:World) clients (serverSocket:NetServer) =

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