module Server

open Microsoft.Xna.Framework
open System.Collections
open System.Collections.Generic
open Lidgren.Network

open HelperFunctions
open SharedServerClient
open ECS
open ECSTypes
open ECSNetworkServer
open ServerTypes

let ComponentDiffMask (entity : Entity) (previous : EntityManager) (current : EntityManager) =
    let willSync x = (current.network.[entity] &&& (uint32 x)) <> 0u
    let mutable componentDiffMask = 0u

    if ComponentBit.Appearance |> willSync then
        if (not <| previous.appearance.ContainsKey(entity) || 
                (previous.appearance.[entity] <> current.appearance.[entity])) then
            componentDiffMask <- componentDiffMask + uint32 ComponentBit.Appearance
    if ComponentBit.Position |> willSync then
        if (not <| previous.position.ContainsKey(entity) || 
                (previous.position.[entity] <> current.position.[entity])) then
            componentDiffMask <- componentDiffMask + uint32 ComponentBit.Position 
    if ComponentBit.Velocity |> willSync then
        if (not <| previous.position.ContainsKey(entity) || 
                (previous.velocity.[entity] <> current.velocity.[entity])) then
            componentDiffMask <- componentDiffMask + uint32 ComponentBit.Velocity

    componentDiffMask

let DeltaSnapshot (client : Client) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()

    // Write delta compressed entity existence bit string
    netBuffer.Write(NetBufferEntityChunks client baseline)
    
    //Get latest acked snapshot
    let latestAckedSnapshot = 
        match client.snapshots |> List.rev |> List.tryFind (fun x -> x.clientAcknowledged = true) with
            | Some x -> x
            | None -> DummySnapshot()
        
    // Write delta compressed entities
    let mutable numberOfEntitiesToSend = 0
    let tempBuffer = new NetBuffer()
    for entity in baseline.entities do
        let componentDiffMask = ComponentDiffMask entity latestAckedSnapshot.entityManager baseline
        if componentDiffMask <> 0u then
            numberOfEntitiesToSend <- numberOfEntitiesToSend + 1
            tempBuffer.Write(componentDiffMask)
            tempBuffer.Write(NetBufferEntityComponents entity componentDiffMask baseline)

    netBuffer.Write(numberOfEntitiesToSend)
    netBuffer.Write(tempBuffer)
    
    netBuffer

let SendSnapshotToClients serverEntityManager (clients : Client list) (serverSocket : NetServer) =

    for clientSnapshotTuple in clients do
        let message = serverSocket.CreateMessage()
        let netBuffer = new NetBuffer()
        let client = fst clientSnapshotTuple
        let snapshots = snd clientSnapshotTuple

        netBuffer.Write(byte ServerMessage.Snapshot)
        //get the last acked snapshot for client to diff with baseline
        let lastAckedSnapshot =
            let mutable lastAckedSnapshot' = dummySnapshot()

            for snapshot in snapshots do
                if snapshot.clientAcknowledged then
                    lastAckedSnapshot' <- snapshot
            lastAckedSnapshot'


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

