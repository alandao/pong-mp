module Server

open Microsoft.Xna.Framework
open System.Collections
open System.Collections.Generic
open System.Collections.Specialized
open Lidgren.Network

open HelperFunctions
open SharedServerClient
open ECS
open ECSTypes
open ECSNetworkServer
open ServerTypes

let DeltaEntity (entity : Entity) (snapshot : Snapshot) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()

    netBuffer.Write(entity)

    let willSync x = (baseline.network.[entity] &&& (uint32 x)) <> 0u
    let mutable componentDiffMask = 0u

    if ComponentBit.Appearance |> willSync then
        if (not <| snapshot.entities.appearance.ContainsKey(entity) || 
                (snapshot.entities.appearance.[entity] <> baseline.appearance.[entity])) then
            componentDiffMask <- componentDiffMask + uint32 ComponentBit.Appearance
    if ComponentBit.Position |> willSync then
        if (not <| snapshot.entities.position.ContainsKey(entity) || 
                (snapshot.entities.position.[entity] <> baseline.position.[entity])) then
            componentDiffMask <- componentDiffMask + uint32 ComponentBit.Position 
    if ComponentBit.Velocity |> willSync then
        if (not <| snapshot.entities.position.ContainsKey(entity) || 
                (snapshot.entities.velocity.[entity] <> baseline.velocity.[entity])) then
            componentDiffMask <- componentDiffMask + uint32 ComponentBit.Velocity

    netBuffer.Write(componentDiffMask)

    let needsUpdate x = (componentDiffMask &&& (uint32 x)) <> 0u

    if ComponentBit.Appearance |> needsUpdate then
        let appr = baseline.appearance.[entity]
        netBuffer.Write(NetBufferAppearance appr)
    if ComponentBit.Position |> needsUpdate then
        let pos = baseline.position.[entity]
        netBuffer.Write(NetBufferPosition pos)
    if ComponentBit.Velocity |> needsUpdate then
        let vel = baseline.velocity.[entity]
        netBuffer.Write(NetBufferVelocity vel)

    netBuffer

let NetBufferEntityChunks (client : Client) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()

    //chunks which weren't acked will be resent with latest chunk data
    let updateFlag = new BitArray(baseline.entityChunkUpdateFlag)

    let snapshotsAfterLatestAcked = 
        let reversedSnapshots = List.rev client.snapshots
        Seq.takeWhile (fun x -> x.clientAcknowledged = false) reversedSnapshots
            
    for snapshot in snapshotsAfterLatestAcked do
        updateFlag.Or(updateFlag) |> ignore

    //write chunk update flags
    for x in updateFlag do
        netBuffer.Write(x)

    //write chunks
    let mutable i = 0
    for x in updateFlag do
        if x = true then
            for y in baseline.entityChunks.[i] do
                netBuffer.Write(y)
        i <- i + 1
        
let DeltaSnapshot (snapshot : Snapshot) (baseline : EntityManager) =
    let netBuffer = new NetBuffer()

    // Write delta compressed entity existence bit string
    for i = 0  to entityMaxCount do
        

    

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

