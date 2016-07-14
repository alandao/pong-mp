module Server

open Microsoft.Xna.Framework
open System.Collections.Generic
open Lidgren.Network

open HelperFunctions
open SharedServerClient
open ECS
open ECSTypes

//Types

//ComponentSyncMask determines what components of the entity we want to sync to the clients
type SyncedComponentMask = int
type ServerEntityManager =
    { 
        entityManager : EntityManager
        //all entity keys belong in entityManager
        synchronizedEntities : Dictionary<Entity, SyncedComponentMask>
    }


//  SYSTEMS

//Clients will first receive a schema update before receiving world updates 
let SendFullSchemaToClients serverEntityManager clients (serverSocket:NetServer) =
    let message = serverSocket.CreateMessage()
    let netBuffer = new NetBuffer()
    
    netBuffer.Write(byte ServerMessage.Schema)
    netBuffer.Write(serverEntityManager.synchronizedEntities.Count)
    for entMaskPair in serverEntityManager.synchronizedEntities do
        netBuffer.Write(entMaskPair.Key.ToByteArray())
        netBuffer.Write(entMaskPair.Value)
    netBuffer.Write("Schema Update okay!")
    message.Write(netBuffer)

    for client in clients do
        serverSocket.SendMessage(message, client, NetDeliveryMethod.Unreliable) |> ignore
        
let SendFullGameState serverEntityManager clients (serverSocket:NetServer) =
    let WriteSyncedComponents entMaskPair netBuffer =
        let syncedComponentMask = entMaskPair.Value
        if (syncedComponentMask &&& ComponentBit.Appearance)
            netBuffer.Write()

    //for each client, send a full snapshot of the gamestate
    for client in clients do
        let message = serverSocket.CreateMessage()
        let netBuffer = new NetBuffer()
        netBuffer.Write(byte ServerMessage.Snapshot)
        for entMaskPair in serverEntityManager.synchronizedEntities do
            entMaskPair.Value
            //netBuffer.Write(entity)
            
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

let ProcessClientMessages currentClients (serverSocket:NetServer) =
    let mutable newClients:NetConnection list = []

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
                newClients <- message.SenderConnection::newClients
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

